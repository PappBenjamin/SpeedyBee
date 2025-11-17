# SpeedyBee Recording Save/Load Feature

## Overview
The application now supports saving and loading recordings to/from both CSV files and PostgreSQL database through a FastAPI backend.

## Features Implemented

### 1. Save Recording Dialog
When stopping a recording, you now have two options:
- **Save as CSV**: Traditional file-based storage
- **Save to PostgreSQL**: Database storage with searchable names

### 2. Load Recording Dialog
When loading a recording, you can choose from:
- **Load from CSV**: Browse and select a CSV file
- **Load from PostgreSQL**: Search and select from saved recordings in the database

### 3. API Service
A new `ApiService` class handles all HTTP communication with the FastAPI backend for:
- Saving runs with frames
- Searching/listing runs
- Loading specific run details
- Deleting runs (optional)

## FastAPI Backend Setup

### Required Python Packages
```bash
pip install fastapi uvicorn sqlalchemy psycopg2-binary pydantic
```

### Example FastAPI Backend (`main.py`)

```python
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from sqlalchemy import create_engine, Column, Integer, String, DateTime, ForeignKey
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, relationship
from datetime import datetime
from pydantic import BaseModel
from typing import List
import os

# Database setup
DATABASE_URL = os.getenv("DATABASE_URL", "postgresql://username:password@localhost:5432/speedybee")
engine = create_engine(DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()

# Models
class Run(Base):
    __tablename__ = "runs"
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String, index=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    frames = relationship("Frame", back_populates="run", cascade="all, delete-orphan")

class Frame(Base):
    __tablename__ = "frames"
    id = Column(Integer, primary_key=True, index=True)
    run_id = Column(Integer, ForeignKey("runs.id"))
    frame_number = Column(Integer)
    accel_x = Column(Integer)
    accel_y = Column(Integer)
    accel_z = Column(Integer)
    gyro_x = Column(Integer)
    gyro_y = Column(Integer)
    gyro_z = Column(Integer)
    run = relationship("Run", back_populates="frames")

Base.metadata.create_all(bind=engine)

# Pydantic models
class FrameData(BaseModel):
    accel_x: int = Field(alias="AccelX")
    accel_y: int = Field(alias="AccelY")
    accel_z: int = Field(alias="AccelZ")
    gyro_x: int = Field(alias="GyroX")
    gyro_y: int = Field(alias="GyroY")
    gyro_z: int = Field(alias="GyroZ")
    frame_number: int = Field(alias="FrameNumber")
    
    class Config:
        populate_by_name = True

class SaveRunRequest(BaseModel):
    name: str = Field(alias="Name")
    frames: List[FrameData] = Field(alias="Frames")
    
    class Config:
        populate_by_name = True

class RunSummary(BaseModel):
    id: int = Field(alias="Id")
    name: str = Field(alias="Name")
    frame_count: int = Field(alias="FrameCount")
    created_at: datetime = Field(alias="CreatedAt")
    
    class Config:
        populate_by_name = True

class RunDetails(BaseModel):
    id: int = Field(alias="Id")
    name: str = Field(alias="Name")
    created_at: datetime = Field(alias="CreatedAt")
    frames: List[FrameData] = Field(alias="Frames")
    
    class Config:
        populate_by_name = True

# FastAPI app
app = FastAPI()

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.post("/api/runs", response_model=RunSummary)
def create_run(request: SaveRunRequest):
    db = SessionLocal()
    try:
        run = Run(name=request.name)
        db.add(run)
        db.flush()
        
        for frame_data in request.frames:
            frame = Frame(
                run_id=run.id,
                frame_number=frame_data.frame_number,
                accel_x=frame_data.accel_x,
                accel_y=frame_data.accel_y,
                accel_z=frame_data.accel_z,
                gyro_x=frame_data.gyro_x,
                gyro_y=frame_data.gyro_y,
                gyro_z=frame_data.gyro_z
            )
            db.add(frame)
        
        db.commit()
        db.refresh(run)
        
        return RunSummary(
            Id=run.id,
            Name=run.name,
            FrameCount=len(request.frames),
            CreatedAt=run.created_at
        )
    except Exception as e:
        db.rollback()
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        db.close()

@app.get("/api/runs", response_model=List[RunSummary])
def get_runs(search: str = None):
    db = SessionLocal()
    try:
        query = db.query(Run)
        if search:
            query = query.filter(Run.name.ilike(f"%{search}%"))
        
        runs = query.order_by(Run.created_at.desc()).all()
        
        return [
            RunSummary(
                Id=run.id,
                Name=run.name,
                FrameCount=len(run.frames),
                CreatedAt=run.created_at
            )
            for run in runs
        ]
    finally:
        db.close()

@app.get("/api/runs/{run_id}", response_model=RunDetails)
def get_run(run_id: int):
    db = SessionLocal()
    try:
        run = db.query(Run).filter(Run.id == run_id).first()
        if not run:
            raise HTTPException(status_code=404, detail="Run not found")
        
        frames = sorted(run.frames, key=lambda f: f.frame_number)
        
        return RunDetails(
            Id=run.id,
            Name=run.name,
            CreatedAt=run.created_at,
            Frames=[
                FrameData(
                    AccelX=f.accel_x,
                    AccelY=f.accel_y,
                    AccelZ=f.accel_z,
                    GyroX=f.gyro_x,
                    GyroY=f.gyro_y,
                    GyroZ=f.gyro_z,
                    FrameNumber=f.frame_number
                )
                for f in frames
            ]
        )
    finally:
        db.close()

@app.delete("/api/runs/{run_id}")
def delete_run(run_id: int):
    db = SessionLocal()
    try:
        run = db.query(Run).filter(Run.id == run_id).first()
        if not run:
            raise HTTPException(status_code=404, detail="Run not found")
        
        db.delete(run)
        db.commit()
        return {"message": "Run deleted successfully"}
    finally:
        db.close()

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
```

### Database Setup

1. Install PostgreSQL
2. Create a database:
```sql
CREATE DATABASE speedybee;
```

3. Update the `DATABASE_URL` in the FastAPI code with your credentials:
```
postgresql://username:password@localhost:5432/speedybee
```

### Running the API

```bash
# From your FastAPI directory
uvicorn main:app --reload
```

The API will be available at `http://localhost:8000`

## Usage

### Saving a Recording
1. Start recording in the Visualization page
2. Click "Stop Recording"
3. A dialog will appear asking for:
   - Recording name
   - Save location (CSV or PostgreSQL)
4. Click "Save"

### Loading a Recording
1. Select "CSV" as data source in Visualization page
2. Click "Select CSV File"
3. A dialog will appear with options to:
   - Browse for a CSV file, OR
   - Search and load from PostgreSQL database
4. Select your recording and click "Load"

## API Endpoints

- `POST /api/runs` - Save a new recording
- `GET /api/runs?search={name}` - List/search recordings
- `GET /api/runs/{id}` - Get recording details with all frames
- `DELETE /api/runs/{id}` - Delete a recording

## Notes

- The FastAPI backend URL is configured in `ApiService.cs` as `http://localhost:8000`
- Make sure the FastAPI server is running before using PostgreSQL save/load features
- CSV files are still supported as a fallback option
- Recording names should be unique and descriptive
