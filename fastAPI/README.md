# FastAPI IMU Data Collection

This directory contains the FastAPI service for collecting IMU data from serial and pushing it directly to PostgreSQL database.

## ✅ Extractor Replacement Complete!

**You can now delete the extractor folder!** All functionality has been replaced:
- ✅ Data generation → `generate_dummy_data.py`
- ✅ Database insertion → `db_pusher.py` and `generate_dummy_data.py`
- ✅ No more Redis queue dependency for basic usage

## Files

- **`main.py`** - FastAPI server with endpoints for data collection
- **`serial_reader.py`** - Shared module for reading and parsing serial data
- **`db_pusher.py`** - Module for pushing data directly to PostgreSQL database
- **`generate_dummy_data.py`** - Generate dummy IMU data and save directly to database

## Endpoints

### `POST /push-to-db`
Reads IMU data from serial and pushes it **directly to PostgreSQL database**.
```bash
curl -X POST http://localhost:8000/push-to-db
```

### `POST /push`
Legacy endpoint - pushes raw serial data to Redis key.

### `POST /push-parsed`
Reads and parses serial data, then pushes to Redis key.

### `GET /get`
Gets data from Redis key.

## Usage

### 1. Start the FastAPI server
```bash
cd fastAPI
uvicorn main:app --reload
```

### 2. Generate dummy test data (Replaces extractor's generate command)
```bash
# Direct to database (recommended - no extractor needed!)
python generate_dummy_data.py -c 100

# Or push to Redis queue only (old way, requires extractor)
python generate_dummy_data.py -c 100 --queue-only
```

### 3. Push real serial data to database
```bash
curl -X POST http://localhost:8000/push-to-db
```

## Environment Variables

Set these for database connection:
```bash
export PG_HOST=localhost
export PG_PORT=5432
export PG_DB=postgres
export PG_USER=user
export PG_PASSWORD=password
```

## What Changed?

### Before (with extractor):
```
Serial → FastAPI → Redis Queue → Extractor → PostgreSQL
```

### After (no extractor needed):
```
Serial → FastAPI → PostgreSQL ✅
Dummy Data Generator → PostgreSQL ✅
```

The extractor is no longer needed for basic operations!
