---
name: react-state-patterns
description: "React state management patterns for ASP.NET Zero CRUD operations"
---

# React State Patterns

## API State Pattern

```typescript
interface ApiState<T> {
    data: T | null;
    loading: boolean;
    error: string | null;
}

function useApiState<T>(initialData: T | null = null): [ApiState<T>, {
    setLoading: () => void;
    setData: (data: T) => void;
    setError: (error: string) => void;
}] {
    const [state, setState] = useState<ApiState<T>>({
        data: initialData,
        loading: false,
        error: null,
    });

    return [state, {
        setLoading: () => setState({ data: null, loading: true, error: null }),
        setData: (data: T) => setState({ data, loading: false, error: null }),
        setError: (error: string) => setState({ data: null, loading: false, error }),
    }];
}
```

## CRUD State

```typescript
const [products, setProducts] = useState<Product[]>([]);
const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
const [showForm, setShowForm] = useState(false);
const [saving, setSaving] = useState(false);
const [loading, setLoading] = useState(false);
```

## Form State

```typescript
const [formData, setFormData] = useState<CreateProductDto>({
    name: '',
    price: 0,
    description: '',
});

const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData((prev) => ({
        ...prev,
        [e.target.name]: e.target.value,
    }));
};

const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
        await apiCall('/Product/CreateProduct', {
            method: 'POST',
            body: JSON.stringify(formData),
        });
        setShowForm(false);
        await fetchProducts();
    } finally {
        setSaving(false);
    }
};
```

## Key Patterns

| Pattern | Hook | Purpose |
|---------|------|---------|
| Loading state | `useState<boolean>(false)` | Show spinner during API calls |
| Error state | `useState<string \| null>(null)` | Display error messages |
| Form data | `useState<Dto>({...})` | Track form input values |
| Selected item | `useState<T \| null>(null)` | Track item being edited |
| Modal visibility | `useState<boolean>(false)` | Show/hide create/edit form |
| Data fetching | `useEffect` + `useCallback` | Load data on mount |
