# Server Profiles

Profiles save server connection details for reuse, eliminating the need to specify URIs and API keys on every connection.

---

## Managing Profiles

Built-in commands under the `server profile` group:

| Command | Description |
|---------|-------------|
| `server profile add` | Save a new profile |
| `server profile list` | List all saved profiles |
| `server profile show` | Show details of a profile |
| `server profile remove` | Delete a profile |
| `server profile set-default` | Set the default profile |
| `server profile set-key` | Update the API key for a profile |

---

## Adding a Profile

```
app> server profile add --name production --uri https://prod.example.com --api-key my-secret-key
Profile 'production' saved.
```

---

## Listing Profiles

```
app> server profile list
  production (default)    https://prod.example.com
  staging                 https://staging.example.com
  local                   http://localhost:5000
```

---

## Setting the Default Profile

```
app> server profile set-default --name production
Default profile set to 'production'.
```

The default profile is used by [auto-connect](auto-connect.md) when no `--profile` argument or `BITPANTRY_PROFILE` environment variable is set.

---

## Connecting by Profile

```
app> server connect --profile production
```

---

## Storage

### Profile Data

Profiles are stored as JSON files in the profiles storage directory:

- Default: `{LocalAppData}/BitPantry/CommandLine/profiles`
- Configurable via `SignalRClientOptions.ProfilesStoragePath`

### Credential Storage

API keys and tokens are stored separately using `CredentialStore`, which encrypts credentials using DPAPI (Windows) or platform-equivalent protection.

---

## The `BITPANTRY_PROFILE` Environment Variable

Set this environment variable to select a profile for non-interactive use:

```shell
export BITPANTRY_PROFILE=production
myapp deploy --environment staging
```

This is the second-priority fallback in the [auto-connect](auto-connect.md) profile resolution order.

---

## See Also

- [Setting Up the Client](index.md)
- [Connecting & Disconnecting](connecting.md)
- [Auto-Connect](auto-connect.md)
- [Server Authentication](../server/authentication.md)
