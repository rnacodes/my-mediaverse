"""
Script Runner FastAPI Application

A lightweight API service for executing and monitoring normalization scripts.

Usage:
    uvicorn api.main:app --host 0.0.0.0 --port 8001 --reload

Or with python:
    python -m api.main
"""

from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from .config import settings
from .routers import health, jobs, ai_test
from .services.job_manager import JobManager


@asynccontextmanager
async def lifespan(app: FastAPI):
    """
    Application lifespan manager.

    Sets up the job manager on startup and cleans up on shutdown.
    """
    # Startup
    app.state.job_manager = JobManager()
    print("Script Runner API started.")
    print(f"API key auth: {'enabled' if settings.api_key else 'disabled'}")
    print(f"Database URL: {'configured' if settings.database_url else 'not configured'}")

    yield

    # Shutdown
    await app.state.job_manager.shutdown()
    print("Script Runner API shutdown complete.")


app = FastAPI(
    title="MyMediaVerse Script Runner",
    description="API for executing and monitoring normalization scripts",
    version="1.0.0",
    lifespan=lifespan
)

# Configure CORS
origins = settings.allowed_origins or [
    "http://localhost:5173",  # Vite dev server
    "http://localhost:5033",  # .NET API
    "http://localhost:3000",  # Alternative frontend port
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include routers
app.include_router(health.router, prefix="/health", tags=["Health"])
app.include_router(jobs.router, prefix="/jobs", tags=["Jobs"])
app.include_router(ai_test.router, prefix="/ai-test", tags=["AI Testing"])


@app.get("/")
async def root():
    """Root endpoint with basic API info."""
    return {
        "service": "MyMediaVerse Script Runner",
        "version": "1.0.0",
        "docs": "/docs",
        "health": "/health"
    }


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "api.main:app",
        host="0.0.0.0",
        port=8001,
        reload=True
    )
