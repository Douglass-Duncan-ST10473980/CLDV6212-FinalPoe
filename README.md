# CoffeeNChill Canteen Management System — CLDV6212 POE

## Overview

This repository contains the CoffeeNChill cloud project, built in three parts for the CLDV6212 Portfolio of Evidence:

- **Part 1:** Azure Functions + Azure Table Storage + Azure File Share (Menu API, Document API)
- **Part 2:** Azure Storage Queue + Queue-Triggered Functions (Order Processing)
- **Part 3:** ASP.NET Core API Gateway + JWT Authentication + VPS Deployment

## Repository Structure

| Folder | Description |
|--------|-------------|
| `CoffeeNChill.Functions/` | Azure Functions project (Parts 1 & 2) |
| `CoffeeNChill.ApiGateway/` | ASP.NET Core API Gateway (Part 3) |
| `docs/postman/` | Postman collections for testing |
| `docker-compose.yml` | Docker orchestration (Azurite + Functions + Gateway) |

## Running Locally

### Prerequisites
- .NET 8 SDK
- Azurite (or Docker)
- Azure Functions Core Tools

### Steps
1. Start Azurite:
azurite


2. Start the Functions host:
cd CoffeeNChill.Functions
func start

3. Start the API Gateway:
cd CoffeeNChill.ApiGateway
dotnet run


4. Open Swagger:
http://localhost:5000/swagger



## Live Deployment (VPS)

- **Live VPS URL:** `http://40.127.11.155:5000/swagger`
- **Docker deployment:** `docker-compose up -d`

## Video Walkthrough

[YouTube link to be added once the final video is recorded]

---

## Part 1 — Menu API & Document API

Built with Azure Functions (.NET 8 Isolated) and Azure Table Storage / File Share. See `CoffeeNChill.Functions/Functions/MenuFunctions.cs` and `DocumentFunctions.cs`.

## Part 2 — Order Queue Processing

Orders are placed on the `order-processing-queue`. A queue-triggered function (`ProcessOrderQueue`) reads each message, saves it to the `Orders` table with status "Received", and manages the full lifecycle: **Received → Preparing → Ready → Collected**. Poison-queue handling ensures failed messages are logged.

## Part 3 — API Gateway (Role 1)

**Author:** Neha Heeralal — ST10478910

### What It Does

The `CoffeeNChill.ApiGateway` is an ASP.NET Core Web API that sits in front of the Azure Functions and exposes a clean REST API to clients. It **forwards** every request to the appropriate backend endpoint and returns the response — so clients only need to know about the gateway, not the underlying services.

### Architecture

| File | Purpose |
|------|---------|
| `Program.cs` | Configures DI, HttpClient, Swagger, and global exception middleware |
| `Controllers/MenuController.cs` | Forwards menu CRUD requests (GET/POST/PUT/DELETE) |
| `Controllers/OrdersController.cs` | Forwards order queue, listing, and status endpoints |
| `Controllers/DocumentsController.cs` | Forwards document upload, list, and download |
| `Middleware/GlobalExceptionHandlingMiddleware.cs` | Catches all unhandled exceptions and returns clean JSON errors |
| `DTOs/MenuItemRequestDto.cs` | Request model for creating/updating menu items |

### Endpoints Exposed by the Gateway

| Method | Route | Forwards To |
|--------|-------|-------------|
| `GET` | `/api/health` | Functions' health check |
| `GET` | `/api/menu` | Functions' get all menu items |
| `GET` | `/api/menu/{category}/{sku}` | Functions' get single menu item |
| `GET` | `/api/categories/{category}` | Functions' get items by category |
| `POST` | `/api/menu` | Functions' create menu item |
| `PUT` | `/api/menu/{category}/{sku}` | Functions' update menu item |
| `DELETE` | `/api/menu/{category}/{sku}` | Functions' delete menu item |
| `POST` | `/api/orders/queue` | Functions' queue order (producer) |
| `GET` | `/api/orders` | Functions' get all orders |
| `GET` | `/api/orders/{orderId}` | Functions' get single order |
| `PUT` | `/api/orders/status` | Functions' advance order status |
| `PUT` | `/api/orders/{orderId}/status` | Functions' set specific order status |
| `POST` | `/api/documents/upload` | Functions' upload staff document |
| `GET` | `/api/documents` | Functions' list staff documents |
| `GET` | `/api/documents/download/{fileName}` | Functions' download staff document |

### Configuration

The Gateway reads these settings from `appsettings.json` (locally) or Docker environment variables (in the container stack):

| Key | Local Dev | Docker |
|-----|-----------|--------|
| `Functions:BaseUrl` | `http://localhost:7071/api` | `http://functions:80/api` |
| `ConnectionStrings:AzureStorage` | `UseDevelopmentStorage=true` | Azurite connection string |
| `Jwt:Key` / `Jwt:Issuer` / `Jwt:Audience` | Placeholders | From `.env` file |

### Swagger UI

The Gateway exposes interactive API documentation at:
- **Local:** `http://localhost:5000/swagger`
- **Docker/VPS:** `http://40.127.11.155:5000/swagger`

Swagger is available in **both** Development and Production (it's registered outside the `if (app.Environment.IsDevelopment())` block).

### Global Exception Handling

Every unhandled exception is caught by `GlobalExceptionHandlingMiddleware`, which:
- Logs the error with method + path
- Returns a clean JSON response: `{ "error": "...", "path": "...", "method": "...", "traceId": "..." }`
- Prevents raw stack traces from leaking to clients

### Postman Collection

A Postman collection for testing the Gateway is available at:
`docs/postman/CoffeeNChill - API Gateway.postman_collection.json`

Import it into Postman, ensure the Functions host is running (`http://localhost:7071`) and the Gateway is running (`http://localhost:5000`), then test each request.

### Docker Notes

The Gateway is containerized (see `CoffeeNChill.ApiGateway/Dockerfile` and `docker-compose.yml`). It runs on **port 5000** inside the Docker network. The Functions are accessible internally at `http://functions:80/api` — which is why `UseHttpsRedirection()` was removed (the Docker network uses plain HTTP).
---
