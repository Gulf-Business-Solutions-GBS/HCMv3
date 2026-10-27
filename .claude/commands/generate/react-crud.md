---
description: "Generate React CRUD page with Ant Design table/modal, Metronic layout, NSwag service proxies, and permission guards"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
argument-hint: "$ENTITY_NAME - The entity to generate React CRUD for (e.g., Product)"
---

# Generate React CRUD

## Usage

`/generate:react-crud $ARGUMENTS`

## Prerequisites

- Backend app service and DTOs must exist (with Swagger endpoint available)
- NSwag service proxies must be regenerated if the backend service is new (`react/nswag/refresh.bat`)
- Permissions must be defined in `AppPermissions` and registered in `AppAuthorizationProvider`
- Localization keys must exist in the XML files

## What It Does

1. **Creates list page component** — `pages/admin/{feature}/index.tsx` with Ant Design `Table`, `PageHeader`, filters, action `Dropdown` menus
2. **Creates create/edit modal** — `pages/admin/{feature}/components/CreateOrEditModal.tsx` with Ant Design `Modal`, `react-hook-form`, and service proxy calls
3. **Adds route entry** — Lazy-loaded `React.lazy()` import + `<Route>` in `AppRouter.tsx`
4. **Adds menu entry** — Item with `permissionName` in `appNavigation.tsx`
5. **Wires permission guards** — `usePermissions()` for conditional rendering of buttons and action menus

## Generated Files

```
react/src/pages/admin/{feature}/
  index.tsx                                   # List page with table
  components/
    CreateOrEdit{Feature}Modal.tsx             # Create/edit modal dialog
```

## Modified Files

```
react/src/routes/AppRouter.tsx                # Add lazy import + <Route>
react/src/lib/navigation/appNavigation.tsx    # Add menu item with permission
```

## Implementation Steps

### Step 1: Create List Page (`pages/admin/{feature}/index.tsx`)

Follow the existing pattern from pages like `roles/index.tsx` or `users/index.tsx`:

```tsx
import React, { useState, useCallback, useEffect } from "react";
import { Table, Dropdown, App } from "antd";
import type { MenuProps } from "antd";
import {
  {Feature}ServiceProxy,
  {Feature}ListDto,
  // Import GetAll input DTO if paginated
} from "@api/generated/service-proxies";
import { usePermissions } from "@hooks/usePermissions";
import { useServiceProxy } from "@/api/service-proxy-factory";
import { useDataTable } from "@hooks/useDataTable";  // For paginated tables
import { useTheme } from "@/hooks/useTheme";
import PageHeader from "../components/common/PageHeader";
import CreateOrEdit{Feature}Modal from "./components/CreateOrEdit{Feature}Modal";
import L from "@/lib/L";

const {Feature}Page: React.FC = () => {
  const { isGranted, isGrantedAny } = usePermissions();
  const {feature}Service = useServiceProxy({Feature}ServiceProxy, []);
  const { modal } = App.useApp();
  const { containerClass } = useTheme();

  // Modal state
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [editingId, setEditingId] = useState<number | undefined>();

  // --- For PAGINATED tables (server-side paging) use useDataTable: ---
  const fetchFn = useCallback(
    (skipCount: number, maxResultCount: number, sorting: string) => {
      return {feature}Service.getAll(new GetAll{Feature}sInput({ skipCount, maxResultCount, sorting }));
    },
    [{feature}Service]
  );
  const { records, loading, pagination, handleTableChange, fetchData } =
    useDataTable<{Feature}ListDto>(fetchFn);

  useEffect(() => { fetchData(); }, []);

  // --- For NON-PAGINATED tables (load all at once, like Roles): ---
  // const [records, setRecords] = useState<{Feature}ListDto[]>([]);
  // const [loading, setLoading] = useState(true);
  // const fetchData = useCallback(async () => {
  //   setLoading(true);
  //   try {
  //     const result = await {feature}Service.getAll();
  //     setRecords(result.items ?? []);
  //   } finally { setLoading(false); }
  // }, [{feature}Service]);
  // useEffect(() => { fetchData(); }, [fetchData]);

  const delete{Feature} = (record: {Feature}ListDto) => {
    modal.confirm({
      title: L("AreYouSure"),
      content: L("{Feature}DeleteWarningMessage", record.name),
      onOk: async () => {
        await {feature}Service.delete(record.id!);
        abp.notify.success(L("SuccessfullyDeleted"));
        fetchData();
      },
    });
  };

  const getMenuItems = (record: {Feature}ListDto): MenuProps => {
    const items: MenuProps["items"] = [];
    if (isGranted("Pages.{Feature}s.Edit")) {
      items.push({
        key: "edit",
        label: L("Edit"),
        onClick: () => { setEditingId(record.id); setIsModalVisible(true); },
      });
    }
    if (isGranted("Pages.{Feature}s.Delete")) {
      items.push({
        key: "delete",
        label: L("Delete"),
        onClick: () => delete{Feature}(record),
        danger: true,
      });
    }
    return { items };
  };

  const columns = [
    {
      title: L("Actions"),
      width: 130,
      align: "center" as const,
      hidden: !isGrantedAny("Pages.{Feature}s.Edit", "Pages.{Feature}s.Delete"),
      render: (_: unknown, record: {Feature}ListDto) => (
        <Dropdown menu={getMenuItems(record)} trigger={["click"]} placement="bottomLeft">
          <button type="button" className="btn btn-primary btn-sm dropdown-toggle d-flex align-items-center">
            <i className="fa fa-cog" />
            <span className="ms-2">{L("Actions")}</span>
          </button>
        </Dropdown>
      ),
    },
    { title: L("Name"), dataIndex: "name", sorter: true },
    // Add more columns as needed
  ];

  return (
    <div>
      <PageHeader
        title={L("{Feature}s")}
        actions={
          isGranted("Pages.{Feature}s.Create") && (
            <button type="button" className="btn btn-primary d-flex align-items-center"
              onClick={() => { setEditingId(undefined); setIsModalVisible(true); }}>
              <i className="fa fa-plus btn-md-icon" />
              <span className="d-none d-md-inline-block"> {L("CreateNew{Feature}")}</span>
            </button>
          )
        }
      />
      <div className={containerClass}>
        <div className="card card-custom">
          <div className="card-body">
            {/* Optional filter row */}
            <Table
              rowKey="id"
              dataSource={records}
              columns={columns}
              loading={loading}
              pagination={pagination}                           // omit for non-paginated
              onChange={(pag, _, sorter) => handleTableChange(pag, sorter)}  // omit for non-paginated
            />
          </div>
        </div>
      </div>
      <CreateOrEdit{Feature}Modal
        isVisible={isModalVisible}
        onClose={() => setIsModalVisible(false)}
        onSave={fetchData}
        {feature}Id={editingId}
      />
    </div>
  );
};
export default {Feature}Page;
```

### Step 2: Create Modal (`pages/admin/{feature}/components/CreateOrEdit{Feature}Modal.tsx`)

Follow the pattern from `CreateOrEditRoleModal.tsx`:

```tsx
import React, { useState, useEffect, useRef } from "react";
import { Modal } from "antd";
import { useForm } from "react-hook-form";
import {
  {Feature}ServiceProxy,
  Create{Feature}Input,   // or CreateOrUpdate{Feature}Input
  {Feature}EditDto,
  Get{Feature}ForEditOutput,
} from "@api/generated/service-proxies";
import { useServiceProxy } from "@api/service-proxy-factory";
import { useDelayedFocus } from "@/hooks/useDelayedFocus";
import L from "@/lib/L";

interface Props {
  isVisible: boolean;
  onClose: () => void;
  onSave: () => void;
  {feature}Id?: number;
}

const CreateOrEdit{Feature}Modal: React.FC<Props> = ({ isVisible, onClose, onSave, {feature}Id }) => {
  const { handleSubmit, register, reset, formState: { errors, isValid } } =
    useForm<{Feature}EditDto>({ mode: "onChange" });
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(false);
  const firstInputRef = useRef<HTMLInputElement | null>(null);
  const delayedFocus = useDelayedFocus();
  const {feature}Service = useServiceProxy({Feature}ServiceProxy, []);

  useEffect(() => {
    if (!isVisible) return;
    setLoading(true);
    {feature}Service.get{Feature}ForEdit({feature}Id).then((result: Get{Feature}ForEditOutput) => {
      reset(result.{feature});
      setLoading(false);
    });
  }, [isVisible, {feature}Id, {feature}Service, reset]);

  const onSubmit = async (values: {Feature}EditDto) => {
    setSaving(true);
    try {
      const input = new Create{Feature}Input();
      input.{feature} = values;
      await {feature}Service.createOrUpdate{Feature}(input);
      onSave();
      onClose();
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title={{feature}Id ? L("Edit{Feature}") : L("CreateNew{Feature}")}
      open={isVisible}
      onCancel={onClose}
      afterOpenChange={(open) => { if (open) delayedFocus(firstInputRef); }}
      width={800}
      footer={[
        <button key="cancel" type="button" className="btn btn-light-primary fw-bold"
          onClick={onClose} disabled={saving}>{L("Cancel")}</button>,
        <button key="save" type="submit"
          className="btn btn-primary fw-bold d-inline-flex align-items-center"
          onClick={handleSubmit(onSubmit)} disabled={!isValid || saving}>
          <i className="fa fa-save align-middle me-2" />
          <span className="align-middle">{L("Save")}</span>
        </button>,
      ]}
    >
      <div className="form" aria-disabled={loading}>
        <div className="mb-5">
          <label className="form-label required" htmlFor="{Feature}Name">{L("Name")}</label>
          {(() => {
            const { ref: nameRef, ...nameRegister } = register("name", { required: true, maxLength: 256 });
            return (
              <input id="{Feature}Name" type="text"
                className={`form-control ${errors.name ? "is-invalid" : ""}`}
                disabled={loading} {...nameRegister}
                ref={(el) => { nameRef(el); firstInputRef.current = el; }}
                maxLength={256} />
            );
          })()}
          {errors.name && <div className="invalid-feedback">{L("ThisFieldIsRequired")}</div>}
        </div>
        {/* Add more fields as needed */}
      </div>
    </Modal>
  );
};
export default CreateOrEdit{Feature}Modal;
```

### Step 3: Register Route in `AppRouter.tsx`

Add at the top with other lazy imports:
```tsx
const {Feature}Page = React.lazy(() => import("../pages/admin/{feature}"));
```

Add inside the `<Route path="admin" ...>` children:
```tsx
<Route path="{feature}" element={<{Feature}Page />} />
```

### Step 4: Add Menu Entry in `appNavigation.tsx`

Add to the appropriate section in `buildRawMenu()`:
```tsx
{
  id: "{Feature}s",
  title: L("{Feature}s"),
  permissionName: "Pages.{Feature}s",
  icon: "some-keenicon",    // Use Keenicons icon name
  route: "/app/admin/{feature}",
},
```

## Key Conventions

### Imports
- Service proxies: `import { ...ServiceProxy, ...Dto } from "@api/generated/service-proxies"`
- Factory hook: `import { useServiceProxy } from "@/api/service-proxy-factory"`
- Hooks: `import { usePermissions } from "@hooks/usePermissions"` / `import { useDataTable } from "@hooks/useDataTable"`
- Localization: `import L from "@/lib/L"`
- Theme: `import { useTheme } from "@/hooks/useTheme"`

### UI Components
- Tables: Ant Design `Table` (with `useDataTable` for server-side pagination)
- Modals: Ant Design `Modal` (controlled `open` prop, parent manages visibility)
- Tabs: Ant Design `Tabs` (for multi-tab modals or pages)
- Forms: `react-hook-form` with `mode: "onChange"`, Bootstrap CSS classes
- Buttons: HTML `<button>` with Bootstrap/Metronic CSS classes (not Ant Design `Button`)
- Action menus: Ant Design `Dropdown` with `MenuProps`
- Confirmations: `modal.confirm()` from `App.useApp()`
- Notifications: `abp.notify.success()`, `abp.notify.error()`
- Layout wrapper: `<div className={containerClass}>` from `useTheme()`

### Patterns
- All components are **functional** with hooks (no class components)
- All text uses `L("Key")` for localization
- Modal visibility controlled by parent via `isVisible` / `onClose` / `onSave` props
- Delete actions wrapped in `modal.confirm()` with `onOk` async handler
- Permission checks via `isGranted("Pages.{Feature}s.Action")`
- Action column hidden when user has no edit/delete permissions: `hidden: !isGrantedAny(...)`
- `useEffect` with `fetchData()` to load initial data
- Page wrapped with `PageHeader` + `containerClass` div + `card card-custom` div

## Skills Used

| Skill | Purpose |
|-------|---------|
| `react-component-patterns` | Component structure, hooks, TypeScript |
| `react-state-patterns` | State management with Redux + hooks |
| `react-permission-patterns` | Permission-based UI rendering |

## Example

```
/generate:react-crud Product
```

Generates:
- `react/src/pages/admin/products/index.tsx` — Products list page
- `react/src/pages/admin/products/components/CreateOrEditProductModal.tsx` — Create/edit modal
- Updates `AppRouter.tsx` with lazy-loaded route
- Updates `appNavigation.tsx` with menu entry
