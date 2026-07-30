from sqlalchemy import create_engine, event
from sqlalchemy.engine import Engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker
import os

_BACKEND_DIR = os.path.dirname(os.path.abspath(__file__))
_DB_PATH = os.path.join(os.path.dirname(_BACKEND_DIR), "gcs_data.db")
DATABASE_URL = "sqlite:///" + _DB_PATH.replace("\\", "/")

engine = create_engine(
    DATABASE_URL, connect_args={"check_same_thread": False}
)

# Enable foreign keys in SQLite
@event.listens_for(Engine, "connect")
def set_sqlite_pragma(dbapi_connection, connection_record):
    cursor = dbapi_connection.cursor()
    cursor.execute("PRAGMA foreign_keys=ON")
    cursor.close()

SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

Base = declarative_base()

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
