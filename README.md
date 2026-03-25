# Budget App

A budget tracking web app built with Spring Boot and React.

## Prerequisites

**Mac**
- Java 17+: `brew install temurin@17`
- Maven: `brew install maven`
- Node.js: `brew install node`
- Docker: [OrbStack](https://orbstack.dev/) (recommended) or [Docker Desktop](https://www.docker.com/products/docker-desktop/)

**Windows**
- Java 17+: [Adoptium](https://adoptium.net/)
- Maven: [maven.apache.org](https://maven.apache.org/install.html)
- Node.js: [nodejs.org](https://nodejs.org/)
- Docker: [Docker Desktop](https://www.docker.com/products/docker-desktop/)

**Linux**
- Java 17+: `sudo apt install temurin-17-jdk` (or distro equivalent)
- Maven: `sudo apt install maven`
- Node.js: `sudo apt install nodejs npm`
- Docker: [docs.docker.com/engine/install](https://docs.docker.com/engine/install/)

## Setup

### 1. Start PostgreSQL

```bash
docker run -d --name postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=password \
  -e POSTGRES_DB=budget_app \
  -p 5432:5432 \
  postgres:17
```

### 2. Install frontend dependencies

```bash
cd frontend && npm install
```

## Running

Start both servers in separate terminals from the project root.

**Backend** (http://localhost:8080):
```bash
mvn spring-boot:run
```

**Frontend** (http://localhost:5173):
```bash
cd frontend && npm run dev
```

## Login

Default credentials (configurable in `src/main/resources/application.properties`):

| Field    | Value           |
|----------|-----------------|
| Email    | user@budget.app |
| Password | password        |
