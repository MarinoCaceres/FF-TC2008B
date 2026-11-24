FROM python:3.12-slim

ENV PYTHONUNBUFFERED=1 PYTHONDONTWRITEBYTECODE=1
WORKDIR /srv

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app ./app
COPY config ./config
COPY run.py .

RUN useradd --system --create-home --uid 10001 appuser
USER appuser

EXPOSE 8000
CMD ["python", "run.py"]
