# Lanzador

App de bandeja del sistema (system tray) para Windows: se ejecuta al iniciar sesión, no muestra
ninguna ventana ni icono en la barra de tareas, y ofrece un menú con accesos rápidos a programas y
páginas web configurables. Se actualiza sola leyendo las releases publicadas en un repositorio de
GitHub.

## Requisitos para desarrollar

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (`winget install Microsoft.DotNet.SDK.8`)

## Estructura del proyecto

```
lanzador/
├─ Lanzador.sln
├─ src/Lanzador/          # Código de la app (WinForms)
│  ├─ Program.cs          # Arranque + hooks de instalación de Velopack
│  ├─ TrayForm.cs         # Icono de bandeja, menú, auto-actualización, recarga en caliente
│  ├─ ConfigManager.cs    # Lee/crea %AppData%\Lanzador\config.json
│  ├─ UpdateService.cs    # Comprobación/descarga de actualizaciones (Velopack + GitHub)
│  ├─ AppSettings.cs      # URL del repo de GitHub para actualizaciones (¡edítalo!)
│  ├─ Models/MenuEntry.cs # Modelo del JSON de configuración
│  └─ config.sample.json  # Plantilla que se copia a %AppData% la primera vez
├─ assets/                # Iconos (app.ico, link.ico)
└─ build/release.ps1      # Script de empaquetado + publicación en GitHub Releases
```

## Configurar el menú (apps y páginas web)

La lista de accesos se lee de:

```
%AppData%\Lanzador\config.json
```

(la primera vez que arranca la app, si el archivo no existe, se crea automáticamente a partir de
`src/Lanzador/config.sample.json`). Desde el propio menú de la bandeja tienes:

- **Editar configuración...** → abre el JSON con el editor asociado (normalmente el Bloc de notas).
- **Recargar menú** → por si prefieres recargar manualmente.

Además, la app vigila el archivo y **recarga el menú automáticamente en cuanto lo guardas**, sin
necesidad de reiniciarla.

### Formato del JSON

```json
{
  "items": [
    { "type": "url", "name": "Gmail", "target": "https://mail.google.com" },
    { "type": "app", "name": "Bloc de notas", "target": "C:\\Windows\\System32\\notepad.exe" },
    { "type": "app", "name": "Mi app", "target": "C:\\Program Files\\MiApp\\app.exe", "args": "--modo-rapido" },
    { "type": "separator" },
    {
      "type": "folder",
      "name": "Ofimática",
      "items": [
        { "type": "url", "name": "Google Docs", "target": "https://docs.google.com" }
      ]
    }
  ]
}
```

Tipos de elemento (`type`):

| type        | Campos usados            | Descripción                                   |
|-------------|---------------------------|------------------------------------------------|
| `url`       | `name`, `target`          | Abre la URL en el navegador por defecto        |
| `app`       | `name`, `target`, `args`  | Lanza un ejecutable (con argumentos opcionales)|
| `folder`    | `name`, `items`           | Submenú anidado con más elementos              |
| `separator` | —                          | Línea separadora                                |

Campo opcional en cualquier `app`/`url`: `"icon": "C:\\ruta\\a\\icono.ico"` para forzar un icono
propio (si no se indica, se usa el icono del propio `.exe` para apps, o un icono genérico de enlace
para URLs).

## Ejecutar en modo desarrollo

```powershell
dotnet run --project src/Lanzador
```

En modo `dotnet run`/depuración, la app **no** está "instalada" por Velopack, así que la
comprobación de actualizaciones se desactiva sola (no falla, simplemente no hace nada) y tampoco se
crea el acceso directo de inicio automático. Eso solo ocurre con la versión empaquetada e instalada
(ver siguiente sección).

## Arranque automático con Windows

Se gestiona solo: al instalarse la app (primera instalación vía el instalador generado por
Velopack), se crea un acceso directo en la carpeta de inicio de Windows (`shell:startup`) que
apunta al ejecutable. Al desinstalarla, ese acceso se elimina automáticamente. No hace falta tocar
nada a mano.

## Publicar una nueva versión (auto-actualización)

La app comprueba actualizaciones cada 6 horas (y también al arrancar) contra las *releases* de un
repositorio de GitHub, usando [Velopack](https://velopack.io/).

### 1. Configura tu repositorio

Edita [`src/Lanzador/AppSettings.cs`](src/Lanzador/AppSettings.cs) y pon la URL real de tu
repositorio de GitHub:

```csharp
public const string UpdateRepoUrl = "https://github.com/TU-USUARIO/TU-REPO";
```

Si el repositorio es **privado**, además define un token en `UpdateRepoToken` (o mejor, no lo
hardcodees: pásalo por variable de entorno si prefieres evitar tenerlo en el código fuente).

### 2. Primera instalación (versión 1.0.0)

```powershell
./build/release.ps1 -Version 1.0.0 -RepoUrl https://github.com/TU-USUARIO/TU-REPO -GithubToken TU_TOKEN
```

Esto compila, empaqueta con `vpk` (genera un instalador `LanzadorSetup.exe` en
`build/Releases/`) y sube la release a GitHub. Descarga y ejecuta `LanzadorSetup.exe` una vez en tu
PC para hacer la primera instalación (esto es lo que crea el acceso de inicio automático y registra
la app para futuras actualizaciones). Las siguientes veces, la propia app se actualiza sola.

El token de GitHub necesita el permiso `repo` (o `public_repo` si el repo es público); créalo en
GitHub → Settings → Developer settings → Personal access tokens. También puedes dejarlo en la
variable de entorno `GITHUB_TOKEN` en vez de pasarlo por parámetro.

### 3. Siguientes versiones

Cambia lo que quieras en el código, y publica con un número de versión superior:

```powershell
./build/release.ps1 -Version 1.0.1 -RepoUrl https://github.com/TU-USUARIO/TU-REPO
```

Todas las instalaciones existentes de Lanzador detectarán la nueva versión (hasta 6 horas después,
o al momento si el usuario pulsa "Buscar actualizaciones ahora" en el menú), la descargarán en
segundo plano y mostrarán un aviso. Con "Reiniciar y actualizar ahora" en el menú se aplica al
momento; si no se pulsa nada, se aplica sola la próxima vez que se reinicie la app o el PC.

## Notas

- El icono de la bandeja y el de la app (`assets/app.ico`) son un diseño de partida muy simple;
  sustitúyelo por el tuyo cuando quieras (mismo archivo, mismo nombre).
- Si algún día quieres una interfaz gráfica para editar el menú en vez de tocar el JSON a mano, el
  sitio natural para añadirla es `TrayForm.OpenConfigForEditing()`.
