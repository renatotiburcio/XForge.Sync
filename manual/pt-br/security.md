# 29 — Segurança

## Dados em Trânsito

Use HTTPS para todos os transportes HTTP:

```csharp
cfg.UseHttp("https://api.example.com/sync"); // Sempre HTTPS
```

## Dados em Repouso

O SQLite local pode ser criptografado:

```csharp
cfg.UseSqlite("./sync.db", options =>
{
    options.EncryptionKey = config["Sync:EncryptionKey"];
});
```

## Autenticação

```csharp
cfg.UseHttp("https://api.example.com/sync", options =>
{
    options.ConfigureRequest = (req, ct) =>
    {
        req.Headers.Authorization = new("Bearer", token);
        return Task.CompletedTask;
    };
});
```

## LGPD

- Dados offline ficam no dispositivo do usuário.
- Sync transmite apenas deltas.
- Implemente exclusão sob demanda.
- Não armazene dados sensíveis sem criptografia.

---

<div align="center">

**Próximo:** [Suporte e Comunidade →](support-community.md)

</div>
