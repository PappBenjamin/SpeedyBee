from fastapi import APIRouter, Body
from fastapi.responses import JSONResponse
from ..services.db import read_postgres, upload_run_to_postgres, query_run_by_name, push_run_to_redis, save_pid_settings, get_latest_pid_settings
import logging

logger = logging.getLogger(__name__)

router = APIRouter()


@router.get("/get-from-postgres")
def get_from_postgres(limit: int = 100):
    """Read IMU data from PostgreSQL database and return via HTTP"""
    result = read_postgres(limit=limit)

    if result["status"] == "ok":
        return result
    else:
        return JSONResponse(content=result, status_code=500)


@router.post("/upload-run")
def upload_run(run_data: dict = Body(...)):
    """
    Upload a recorded run from WPF app to PostgreSQL.

    Expected request body:
    {
        "name": "run_name",
        "timestamp": "2024-01-15T10:30:00Z",
        "frames": [
            {"timestamp": "...", "accel_x": ..., "accel_y": ..., "accel_z": ..., "rot_x": ..., "rot_y": ..., "rot_z": ...},
            ...
        ]
    }
    """
    result = upload_run_to_postgres(run_data)

    if result["status"] == "ok":
        return result
    else:
        return JSONResponse(content=result, status_code=400)


@router.get("/query-run")
def query_run(run_name: str):
    """
    Query PostgreSQL for a specific run by name.
    Returns the run with all its frames and metadata.

    Usage: GET /query-run?run_name=my_run
    """
    result = query_run_by_name(run_name)

    if result["status"] == "ok":
        return result
    elif result["status"] == "not_found":
        return JSONResponse(content=result, status_code=404)
    else:
        return JSONResponse(content=result, status_code=500)


@router.post("/replay-run")
def replay_run(run_name: str = Body(..., embed=True)):
    """
    Push a specific run's frames to Redis queue for replay.

    Request body: { "run_name": "my_run" }
    """
    result = push_run_to_redis(run_name)

    if result["status"] == "ok":
        return result
    else:
        return JSONResponse(content=result, status_code=400)


@router.post("/save-pid-settings")
def save_pid(pid_data: dict = Body(...)):
    """
    Save PID settings to PostgreSQL.

    Expected request body:
    {
        "kp": 1.0,
        "ki": 0.5,
        "kd": 0.1,
        "speed": 100.0
    }
    """
    try:
        kp = float(pid_data.get("kp", 0))
        ki = float(pid_data.get("ki", 0))
        kd = float(pid_data.get("kd", 0))
        speed = float(pid_data.get("speed", 0))

        result = save_pid_settings(kp, ki, kd, speed)

        if result["status"] == "ok":
            return result
        else:
            return JSONResponse(content=result, status_code=500)
    except ValueError as e:
        return JSONResponse(content={"status": "error", "message": f"Invalid parameter type: {str(e)}"}, status_code=400)


@router.get("/get-pid-settings")
def get_pid():
    """
    Get the latest PID settings from PostgreSQL.

    Returns the most recent PID configuration.
    """
    result = get_latest_pid_settings()

    if result["status"] == "ok":
        return result
    elif result["status"] == "no_data":
        return JSONResponse(content=result, status_code=404)
    else:
        return JSONResponse(content=result, status_code=500)
