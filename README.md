# Lanzador

App de bandeja del sistema (system tray) para Windows, pensada para desplegar en equipos de
alumnos: se ejecuta al iniciar sesión, no muestra ninguna ventana ni icono en la barra de tareas, y
al hacer clic en su icono abre un panel con botones de acceso directo a las páginas web y programas
que el profesor haya configurado. **El alumno no puede editar esos accesos**: el listado va
compilado dentro del propio `.exe`. Para cambiarlos, el profesor edita el código, publica una nueva
versión, y esta llega sola a todos los equipos ya instalados mediante auto-actualización (Velopack +
GitHub Releases).

## Requisitos para desarrollar

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (`winget install Microsoft.DotNet.SDK.8`)

## Estructura del proyecto

```
lanzador/
├─ Lanzador.sln
├─ src/Lanzador/            # Código de la app (WinForms)
│  ├─ Program.cs            # Arranque + hooks de instalación de Velopack (acceso de inicio automático)
│  ├─ TrayForm.cs           # Icono de bandeja: clic abre el panel, clic derecho da opciones de admin
│  ├─ LauncherWindow.cs     # Ventana flotante con los botones de acceso
│  ├─ ConfigLoader.cs       # Lee config.json embebido dentro del .exe (recurso compilado)
│  ├─ IconResolver.cs       # Resuelve los iconos de cada botón
│  ├─ Launcher.cs           # Lanza la app/URL de un acceso
│  ├─ UpdateService.cs      # Comprobación/descarga de actualizaciones (Velopack + GitHub)
│  ├─ AppSettings.cs        # URL del repo de GitHub para actualizaciones (¡edítalo!)
│  ├─ Models/MenuEntry.cs   # Modelo del JSON de configuración
│  └─ config.json           # ← AQUÍ es donde el profesor edita los accesos (recurso embebido)
├─ assets/                  # Iconos (app.ico, link.ico)
└─ build/release.ps1        # Script de empaquetado + publicación en GitHub Releases
```

## Configurar los accesos (solo el profesor, en el código)

Edita [`src/Lanzador/config.json`](src/Lanzador/config.json). Este archivo se compila **dentro**
del ejecutable como recurso embebido (no es un archivo suelto que el alumno pueda tocar en su PC).
Cualquier cambio requiere publicar una nueva versión (ver más abajo) para que llegue a los equipos.

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

| type        | Campos usados            | Descripción                                              |
|-------------|---------------------------|-----------------------------------------------------------|
| `url`       | `name`, `target`          | Botón que abre la URL en el navegador por defecto          |
| `app`       | `name`, `target`, `args`  | Botón que lanza un ejecutable (con argumentos opcionales)  |
| `folder`    | `name`, `items`           | Grupo con encabezado y sus propios botones debajo          |
| `separator` | —                          | Línea separadora                                            |

Campo opcional en cualquier `app`/`url`: `"icon": "ruta\\al\\icono.ico"` para forzar un icono
propio (si no se indica, se usa el icono del propio `.exe` para apps, o un icono genérico de enlace
para URLs). Una ruta relativa se resuelve respecto a la carpeta donde está instalado el programa; si
usas iconos propios, cópialos junto al ejecutable añadiéndolos al `.csproj` como `Content`.

## Cómo funciona la interfaz

- **Clic izquierdo** en el icono de la bandeja → abre/cierra el panel con los botones de acceso.
  El panel se cierra solo al hacer clic fuera o pulsar Esc.
- **Clic derecho** → menú reducido solo de administración: "Buscar actualizaciones ahora" y
  "Salir". No hay ninguna opción para editar los accesos.

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

La app comprueba actualizaciones cada 6 horas (y también 15s después de arrancar) contra las
*releases* de un repositorio de GitHub, usando [Velopack](https://velopack.io/).

### 1. Configura tu repositorio

Edita [`src/Lanzador/AppSettings.cs`](src/Lanzador/AppSettings.cs) y pon la URL real de tu
repositorio de GitHub:

```csharp
public const string UpdateRepoUrl = "https://github.com/TU-USUARIO/TU-REPO";
```

Si el repositorio es **privado**, además define un token en `UpdateRepoToken` (ese token viajaría
dentro del `.exe` instalado en cada PC, así que solo dale permiso de lectura). Con un repo
**público** no hace falta ningún token para que la app compruebe actualizaciones.

### 2. Primera instalación

```powershell
./build/release.ps1 -Version 1.0.0 -RepoUrl https://github.com/TU-USUARIO/TU-REPO -GithubToken TU_TOKEN
```

Esto compila, empaqueta con `vpk` (genera un instalador `Lanzador-win-Setup.exe` en
`build/Releases/`) y sube la release a GitHub. Ese instalador es lo que hay que ejecutar una vez en
cada PC de alumno (por ejemplo, distribuido por script de despliegue/GPO/RMM del centro) para hacer
la primera instalación. A partir de ahí, cada equipo se actualiza solo.

El `-GithubToken` solo hace falta para **subir** la release (permiso `repo` si el repositorio es
privado, o `public_repo`/ninguno si es público); no tiene nada que ver con `UpdateRepoToken` del
paso anterior, que es el que necesitarían los propios PCs para *leer* actualizaciones de un repo
privado.

### 3. Siguientes versiones (cambiar los accesos de los alumnos)

Edita `src/Lanzador/config.json` con los nuevos accesos, sube el número de versión y publica:

```powershell
./build/release.ps1 -Version 1.0.1 -RepoUrl https://github.com/TU-USUARIO/TU-REPO
```

Todos los equipos con Lanzador instalado detectarán la nueva versión (hasta 6 horas después, o al
momento si desde el menú de clic derecho se pulsa "Buscar actualizaciones ahora"), la descargarán en
segundo plano y mostrarán un aviso. Con "Reiniciar y actualizar ahora" se aplica al momento; si no
se hace nada, se aplica sola la próxima vez que se reinicie la app o el PC.

## Notas

- El icono de la bandeja y el de la app (`assets/app.ico`) son un diseño de partida muy simple;
  sustitúyelo por el tuyo cuando quieras (mismo archivo, mismo nombre).
- El botón "Salir" del menú de clic derecho cierra la app para esa sesión (volverá a arrancar en el
  siguiente inicio de Windows). Si en el futuro quieres impedir que el alumno la cierre, ese es el
  sitio donde habría que actuar (`TrayForm.ExitApplication`).
