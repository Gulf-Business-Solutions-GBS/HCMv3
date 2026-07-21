---
name: react-component-patterns
description: "React functional component patterns for ASP.NET Zero with Metronic layout, Ant Design, and NSwag service proxies"
---

# React Component Patterns

## Quick Start

React pages in ASP.NET Zero use **functional components** with **TypeScript**, **Ant Design v5** for UI, **Metronic 8** for layout, and **NSwag-generated service proxies** for backend calls:

```tsx
import React, { useState, useCallback } from "react";
import { Table, Button, Space } from "antd";
import type { ColumnsType } from "antd/es/table";
import { App } from "antd";
import { usePermissions } from "@hooks/usePermissions";
import { useServiceProxy } from "@api/service-proxy-factory";
import { useDataTable } from "@hooks/useDataTable";
import { ProductServiceProxy, ProductListDto } from "@api/generated/service-proxies";
import { L } from "@/lib/L";

const ProductsPage: React.FC = () => {
    const { modal } = App.useApp();
    const { isGranted } = usePermissions();
    const productService = useServiceProxy(ProductServiceProxy, []);

    const [modalOpen, setModalOpen] = useState(false);
    const [selectedId, setSelectedId] = useState<number | undefined>();

    const fetchFn = useCallback(
        (skipCount: number, maxResultCount: number, sorting: string, filter: string) =>
            productService.getProducts({ skipCount, maxResultCount, sorting, filter }),
        [productService]
    );

    const { records, loading, pagination, handleTableChange, fetchData } =
        useDataTable<ProductListDto>(fetchFn);

    const handleDelete = (id: number) => {
        modal.confirm({
            title: L("AreYouSure"),
            content: L("ProductDeleteWarningMessage"),
            onOk: async () => {
                await productService.deleteProduct(id); // NSwag flattens EntityDto → id: number for DELETE
                fetchData();
            },
        });
    };

    const columns: ColumnsType<ProductListDto> = [
        { title: L("Name"), dataIndex: "name", sorter: true },
        { title: L("Price"), dataIndex: "price", sorter: true },
        {
            title: L("Actions"),
            key: "actions",
            render: (_, record) => (
                <Space>
                    {isGranted("Pages.Products.Edit") && (
                        <Button
                            size="small"
                            onClick={() => { setSelectedId(record.id); setModalOpen(true); }}
                        >
                            {L("Edit")}
                        </Button>
                    )}
                    {isGranted("Pages.Products.Delete") && (
                        <Button size="small" danger onClick={() => handleDelete(record.id)}>
                            {L("Delete")}
                        </Button>
                    )}
                </Space>
            ),
        },
    ];

    return (
        <div className="card">
            <div className="card-header">
                <h3 className="card-title">{L("Products")}</h3>
                <div className="card-toolbar">
                    {isGranted("Pages.Products.Create") && (
                        <Button
                            type="primary"
                            onClick={() => { setSelectedId(undefined); setModalOpen(true); }}
                        >
                            {L("CreateNewProduct")}
                        </Button>
                    )}
                </div>
            </div>
            <div className="card-body">
                <Table
                    rowKey="id"
                    dataSource={records}
                    columns={columns}
                    loading={loading}
                    pagination={pagination}
                    onChange={(pag, _, sorter) => handleTableChange(pag, sorter)}
                />
            </div>
        </div>
    );
};

export default ProductsPage;
```

## Key Conventions

| Convention | Detail |
|------------|--------|
| Components | Functional with `React.FC`, TypeScript strict mode |
| API calls | `useServiceProxy(ServiceProxy, [])` — NSwag-generated, never raw `fetch` |
| Table state | `useDataTable<Dto>(fetchFn)` — handles paging, sorting, loading |
| Permissions | `const { isGranted } = usePermissions()` |
| Localization | `L("Key")` for all user-facing text |
| Layout | Metronic card structure (`card`, `card-header`, `card-toolbar`, `card-body`) |
| UI components | Ant Design v5 (`Table`, `Button`, `Modal`, `Form`, `Space`, etc.) |
| Delete confirm | `modal.confirm()` from `App.useApp()` |

## Service Proxy Usage

```tsx
// Inside a component (recommended):
const productService = useServiceProxy(ProductServiceProxy, []);

// Outside a component (e.g., in a utility or slice):
import { createServiceProxy } from "@api/service-proxy-factory";
const productService = createServiceProxy(ProductServiceProxy);
```

**CRITICAL:** Never manually edit `src/api/generated/service-proxies.ts`. All proxies are auto-generated by NSwag. To regenerate: run Web.Host, then execute `react/nswag/refresh.bat`.

## Table Pattern

```tsx
const fetchFn = useCallback(
    (skipCount: number, maxResultCount: number, sorting: string, filter: string) =>
        someService.getItems({ skipCount, maxResultCount, sorting, filter }),
    [someService]
);

const { records, loading, pagination, handleTableChange, fetchData } =
    useDataTable<SomeListDto>(fetchFn);

<Table
    rowKey="id"
    dataSource={records}
    columns={columns}
    loading={loading}
    pagination={pagination}
    onChange={(pag, _, sorter) => handleTableChange(pag, sorter)}
/>
```

## Modal Pattern (Create/Edit)

```tsx
import { Modal, Form, Input, InputNumber } from "antd";

interface Props {
    open: boolean;
    id?: number;
    onClose: () => void;
    onSaved: () => void;
}

const CreateOrEditProductModal: React.FC<Props> = ({ open, id, onClose, onSaved }) => {
    const [form] = Form.useForm();
    const [saving, setSaving] = useState(false);
    const productService = useServiceProxy(ProductServiceProxy, []);

    useEffect(() => {
        if (open && id) {
            productService.getProductForEdit(id).then((result) => { // NSwag flattens EntityDto → id: number for GET
                form.setFieldsValue(result.product);
            });
        } else {
            form.resetFields();
        }
    }, [open, id]);

    const handleOk = async () => {
        const values = await form.validateFields();
        setSaving(true);
        try {
            if (id) {
                await productService.updateProduct({ ...values, id });
            } else {
                await productService.createProduct(values);
            }
            onSaved();
        } finally {
            setSaving(false);
        }
    };

    return (
        <Modal
            open={open}
            title={id ? L("EditProduct") : L("CreateNewProduct")}
            onOk={handleOk}
            onCancel={onClose}
            confirmLoading={saving}
            destroyOnClose
        >
            <Form form={form} layout="vertical">
                <Form.Item name="name" label={L("Name")} rules={[{ required: true }]}>
                    <Input />
                </Form.Item>
                <Form.Item name="price" label={L("Price")} rules={[{ required: true }]}>
                    <InputNumber style={{ width: "100%" }} />
                </Form.Item>
            </Form>
        </Modal>
    );
};
```

## Path Aliases

| Alias | Resolves to |
|-------|-------------|
| `@` | `src/` |
| `@api` | `src/api/` |
| `@hooks` | `src/hooks/` |
| `metronic` | `metronic/` |

## File Location

```
react/src/
  app/
    pages/admin/{feature}/
      index.tsx              # Main list page
      CreateOrEdit{Feature}Modal.tsx  # Create/Edit modal
  AppRouter.tsx              # Route registration (lazy-loaded)
  appNavigation.tsx          # Menu entry
```

## Route Registration

```tsx
// AppRouter.tsx — lazy-load all page routes
const ProductsPage = React.lazy(() => import("./pages/admin/products"));

<Route path="/admin/products" element={<ProductsPage />} />
```

## Menu Entry

```tsx
// appNavigation.tsx
{
    title: L("Products"),
    path: "/admin/products",
    permissionName: "Pages.Products",
    icon: "bi bi-box",
}
```

## NSwag Proxy: EntityDto Flattening (CRITICAL)

NSwag generates different TypeScript signatures depending on the HTTP method:

| Backend method | HTTP method | NSwag-generated TS signature |
|----------------|-------------|------------------------------|
| `GetProducts(GetProductsInput)` | POST (via `[HttpPost]`) | `getProducts(body: GetProductsInput)` |
| `GetProductForEdit(EntityDto)` | GET (default) | `getProductForEdit(id: number)` |
| `DeleteProduct(EntityDto)` | DELETE (default) | `deleteProduct(id: number)` |

For HTTP GET and DELETE methods taking `EntityDto`, NSwag **flattens** the DTO to a direct `id: number` parameter.

```tsx
// ❌ WRONG — causes ?Id=[object Object] in the URL
await productService.getProductForEdit(new EntityDto({ id: productId }));
await productService.deleteProduct(new EntityDto({ id: record.id! }));

// ✅ CORRECT — pass the raw number directly
await productService.getProductForEdit(productId);
await productService.deleteProduct(record.id!);
```

`EntityDto` should NOT be imported in React components — it is only used for POST body DTOs.

## Checklist

- [ ] Functional component with TypeScript (`React.FC`)
- [ ] `useServiceProxy()` for all API calls (no raw `fetch`)
- [ ] `useDataTable()` for table paging/sorting/loading
- [ ] `usePermissions()` + `isGranted()` on all action buttons
- [ ] `L()` for all user-facing text (no hardcoded strings)
- [ ] Delete confirmation via `modal.confirm()` from `App.useApp()`
- [ ] Ant Design `Table`, `Modal`, `Form`, `Button`
- [ ] Metronic card layout structure
- [ ] Route registered in `AppRouter.tsx` with `React.lazy()`
- [ ] Menu entry in `appNavigation.tsx` with `permissionName`
