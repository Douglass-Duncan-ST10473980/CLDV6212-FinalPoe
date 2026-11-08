\---



\## Part 3 — API Gateway (Role 1)



\*\*Author:\*\* Neha Heeralal — ST10478910



\### What It Does



The `CoffeeNChill.ApiGateway` is an ASP.NET Core Web API that sits in front of the Azure Functions and exposes a clean REST API to clients. It \*\*forwards\*\* every request to the appropriate backend endpoint and returns the response — so clients only need to know about the gateway, not the underlying services.



\### Architecture



| File | Purpose |

|------|---------|

| `Program.cs` | Configures DI, HttpClient, Swagger, and global exception middleware |

| `Controllers/MenuController.cs` | Forwards menu CRUD requests (GET/POST/PUT/DELETE) |

| `Controllers/OrdersController.cs` | Forwards order queue, listing, and status endpoints |

| `Controllers/DocumentsController.cs` | Forwards document upload, list, and download |

| `Middleware/GlobalExceptionHandlingMiddleware.cs` | Catches all unhandled exceptions and returns clean JSON errors |

| `DTOs/MenuItemRequestDto.cs` | Request model for creating/updating menu items |



\### Endpoints Exposed by the Gateway



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



\### Configuration



The Gateway reads these settings from `appsettings.json` (locally) or Docker environment variables (in the container stack):



| Key | Local Dev | Docker |

|-----|-----------|--------|

| `Functions:BaseUrl` | `http://localhost:7071/api` | `http://functions:80/api` |

| `ConnectionStrings:AzureStorage` | `UseDevelopmentStorage=true` | Azurite connection string |

| `Jwt:Key` / `Jwt:Issuer` / `Jwt:Audience` | Placeholders | From `.env` file |



\### Swagger UI



The Gateway exposes interactive API documentation at:

\- \*\*Local:\*\* `http://localhost:5000/swagger`

\- \*\*Docker/VPS:\*\* `http://40.127.11.155:5000/swagger`



Swagger is available in \*\*both\*\* Development and Production (it's registered outside the `if (app.Environment.IsDevelopment())` block).



\### Global Exception Handling



Every unhandled exception is caught by `GlobalExceptionHandlingMiddleware`, which:

\- Logs the error with method + path

\- Returns a clean JSON response: `{ "error": "...", "path": "...", "method": "...", "traceId": "..." }`

\- Prevents raw stack traces from leaking to clients



\### Postman Collection



A Postman collection for testing the Gateway is available at:

docs/postman/CoffeeNChill - API Gateway.postman\_collection.json





Import it into Postman, ensure the Functions host is running (`http://localhost:7071`) and the Gateway is running (`http://localhost:5000`), then test each request.



\### Docker Notes



The Gateway is containerized (see `CoffeeNChill.ApiGateway/Dockerfile` and `docker-compose.yml`). It runs on \*\*port 5000\*\* inside the Docker network. The Functions are accessible internally at `http://functions:80/api` — which is why `UseHttpsRedirection()` was removed (the Docker network uses plain HTTP).

