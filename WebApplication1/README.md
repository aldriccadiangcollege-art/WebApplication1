# WebApplication1 — Products API

In-memory Products CRUD API with validation, centralized exception handling and standard
responses. Errors are returned as Problem Details.

| Endpoint | Success | Errors |
|---|---|---|
| `GET /api/Product?page=&pageSize=` | 200 (paged `data` / `meta`) | 500 |
| `GET /api/Product/{id}` | 200 | 404, 500 |
| `POST /api/Product` | 201 + `Location` | 400, 409, 500 |
| `PUT /api/Product/{id}` | 200 | 400, 404, 409, 500 |
| `DELETE /api/Product/{id}` | 204 | 404, 500 |