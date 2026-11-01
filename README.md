# Shopora.API

RESTful backend for **Shopora**, a full-stack e-commerce platform. Built with ASP.NET Core, Entity Framework Core and SQL Server, secured with JWT authentication.

> Frontend repository: [shopora-web](https://github.com/moujtazisab/ecommerce-web-react)

<!-- Add a screenshot of Swagger here -->
<!-- ![Swagger UI](docs/swagger.png) -->

## Features

- JWT authentication with role-based authorization (Admin / Customer)
- Product catalog with categories, sub-categories and suppliers
- Orders, order items, payments and shipments management
- Discounts and order cancellations
- Product reviews and ratings
- Admin dashboard statistics and reports
- Server-side pagination, search and filtering
- Global error handling and request validation
- Interactive API documentation with Swagger

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core Web API (.NET 8) |
| Language | C# |
| ORM | Entity Framework Core |
| Database | SQL Server |
| Auth | JWT Bearer |
| Docs | Swagger / OpenAPI |

## Project Structure

```
Shopora.API/
├── Controllers/     # API endpoints
├── Data/            # ApplicationDbContext, migrations
├── Models/          # Entities
├── DTOs/            # Data transfer objects
├── Services/        # Business logic
├── Repositories/    # Data access
├── Mappings/        # Object mapping profiles
├── Middleware/      # Exception handling, etc.
├── Program.cs
└── appsettings.json
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express or Docker)
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**

   ```bash
   git clone https://github.com/YOUR-USERNAME/shopora-api.git
   cd shopora-api
   ```

2. **Configure the connection string and JWT settings**

   Do not commit secrets. Use user-secrets:

   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ECommerceDB;Trusted_Connection=True;TrustServerCertificate=True;"
   dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_SECRET_KEY"
   dotnet user-secrets set "Jwt:Issuer" "Shopora.API"
   dotnet user-secrets set "Jwt:Audience" "Shopora.Web"
   ```

3. **Apply the database migrations**

   ```bash
   dotnet ef database update
   ```

4. **Run the API**

   ```bash
   dotnet run
   ```

   - API: `https://localhost:7001`
   - Swagger: `https://localhost:7001/swagger`

## API Overview

| Resource | Endpoint | Description |
|---|---|---|
| Auth | `/api/auth` | Register, login |
| Users | `/api/users` | User management |
| Categories | `/api/categories` | Product categories |
| SubCategories | `/api/subcategories` | Sub-categories |
| Products | `/api/products` | Product catalog |
| Suppliers | `/api/suppliers` | Suppliers |
| Orders | `/api/orders` | Orders and order items |
| Payments | `/api/payments` | Payments |
| PaymentMethods | `/api/paymentmethods` | Payment methods |
| Shipments | `/api/shipments` | Shipping tracking |
| Discounts | `/api/discounts` | Discount codes |
| Cancellations | `/api/ordercancellations` | Order cancellations |
| Reviews | `/api/reviews` | Product reviews |
| Dashboard | `/api/dashboard` | Statistics |
| Reports | `/api/reports` | Sales reports |

Full documentation is available in Swagger.

## CORS

The API allows requests from the frontend dev server (`http://localhost:5173`). Update the allowed origins in `Program.cs` for production.

## Roadmap

- [ ] Shopping cart
- [ ] Image upload for products
- [ ] Refresh tokens
- [ ] Unit tests (xUnit)
- [ ] Docker and docker-compose
- [ ] CI/CD pipeline
- [ ] Deployment on Azure

## Author

**Saber Moujtazi** - .NET Full Stack Developer

[LinkedIn](https://www.linkedin.com/in/saber-moujtazi-aa4b84323)

## License

This project is licensed under the MIT License.
