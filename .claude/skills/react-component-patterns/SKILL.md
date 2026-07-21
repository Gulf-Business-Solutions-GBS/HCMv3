---
name: react-component-patterns
description: "React functional component patterns for ASP.NET Zero with Metronic layout"
---

# React Component Patterns

## Quick Start

React components in ASP.NET Zero use **functional components** with **TypeScript** and **Metronic** layout:

```tsx
import React, { useState, useEffect, useCallback } from 'react';
import { PageTitle } from '../../../_metronic/layout/core';

interface Product {
    id: number;
    name: string;
    price: number;
}

const ProductsPage: React.FC = () => {
    const [products, setProducts] = useState<Product[]>([]);
    const [loading, setLoading] = useState(false);

    const fetchProducts = useCallback(async () => {
        setLoading(true);
        try {
            const response = await fetch('/api/services/app/Product/GetProducts', {
                headers: { Authorization: `Bearer ${getToken()}` },
            });
            const data = await response.json();
            setProducts(data.result.items);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchProducts();
    }, [fetchProducts]);

    return (
        <>
            <PageTitle>Products</PageTitle>
            <div className="card">
                <div className="card-body">
                    {loading ? (
                        <div>Loading...</div>
                    ) : (
                        <table className="table table-striped">
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Price</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {products.map((product) => (
                                    <tr key={product.id}>
                                        <td>{product.name}</td>
                                        <td>{product.price}</td>
                                        <td>...</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>
            </div>
        </>
    );
};

export default ProductsPage;
```

## Key Conventions

| Convention | Detail |
|------------|--------|
| Components | Functional with `React.FC` |
| State | `useState` hooks |
| Side effects | `useEffect` hooks |
| API calls | `fetch` or `axios` with Bearer token |
| Layout | Metronic components (`PageTitle`, `KTCard`, etc.) |
| Styling | Bootstrap 5 + Metronic classes |
| Types | TypeScript interfaces for all data |

## API Integration

```typescript
const API_BASE = '/api/services/app';

function getToken(): string {
    return localStorage.getItem('enc_auth_token') || '';
}

async function apiCall<T>(url: string, options?: RequestInit): Promise<T> {
    const response = await fetch(`${API_BASE}${url}`, {
        ...options,
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${getToken()}`,
            ...options?.headers,
        },
    });
    const data = await response.json();
    return data.result;
}
```

## File Location

```
react/src/app/pages/{feature}/
  {Feature}Page.tsx          # Main page component
  {Feature}Form.tsx          # Create/edit form
  components/                # Sub-components
```

## Notes

The React frontend in ASP.NET Zero is built on Metronic layout scaffolding. When building new features, follow the existing Metronic patterns found in `react/_metronic/`.

## NSwag Proxy: EntityDto Flattening (CRITICAL)

NSwag generates different TypeScript signatures depending on the HTTP method:

| Backend method | HTTP method | NSwag-generated TS signature |
|---|---|---|
| `GetProducts(GetProductsInput)` | POST (via `[HttpPost]`) | `getProducts(body: GetProductsInput)` |
| `GetProductForEdit(EntityDto)` | GET (default) | `getProductForEdit(id: number)` |
| `DeleteProduct(EntityDto)` | DELETE (default) | `deleteProduct(id: number)` |

For HTTP GET and DELETE methods taking `EntityDto`, NSwag **flattens** the DTO to a direct `id: number` parameter.

**WRONG — causes `?Id=[object Object]` in URL:**
```tsx
// ❌ DO NOT wrap in EntityDto for GET/DELETE methods
await service.getProductForEdit(new EntityDto({ id: productId }));
await service.deleteProduct(new EntityDto({ id: record.id! }));
```

**CORRECT:**
```tsx
// ✅ Pass the raw number directly
await service.getProductForEdit(productId);
await service.deleteProduct(record.id!);
```

This also means `EntityDto` should NOT be imported in React components — it is only needed for POST body DTOs. Remove it from imports if only used for GET/DELETE calls.

## Checklist

- [ ] Functional component with TypeScript
- [ ] Metronic layout integration
- [ ] NSwag service proxies used (not raw fetch)
- [ ] GET/DELETE methods called with raw `id: number`, NOT `new EntityDto({ id })`
- [ ] Loading state management
- [ ] Error handling for API calls
- [ ] Permission checks before rendering actions
