# CryptoView

CryptoView es una aplicación web desarrollada en Blazor Server con .NET 8, orientada al seguimiento, la consulta y el análisis básico de criptomonedas. Permite al usuario gestionar una lista personalizada de activos, consultar precios y variaciones de mercado, registrar notas sobre cada moneda, visualizar gráficos estadísticos y administrar su perfil y preferencias dentro de una interfaz clara y responsive.

---

## Objetivo del proyecto

El objetivo de CryptoView es centralizar la información relevante de criptomonedas en un panel único, permitiendo al usuario:

- Realizar un seguimiento personalizado de activos de interés.
- Consultar precios y variaciones de mercado obtenidos desde una API externa.
- Registrar notas y observaciones por cada criptomoneda.
- Visualizar datos mediante gráficos interactivos.
- Gestionar su perfil, sus preferencias de usuario y el tema de la interfaz.

El proyecto fue desarrollado como trabajo final integrador de la Tecnicatura Superior en Desarrollo de Software.

---

## Funcionalidades principales

- Autenticación de usuarios con hashing seguro de contraseñas (BCrypt).
- Sistema de roles: usuario estándar y administrador. La cuenta de administrador se gestiona de forma interna; el registro público solo crea usuarios con rol "Usuario".
- Dashboard principal con tarjetas de resumen (KPI), métricas de mercado y acceso directo a las secciones del panel.
- Consulta de criptomonedas activas e inactivas con datos de precio, capitalización, volumen y variación.
- Búsqueda y filtrado en tiempo real por nombre o símbolo.
- Sección "Mis Criptomonedas" con listado completo de las monedas guardadas.
- Creación, edición y eliminación de criptomonedas (eliminación lógica mediante marca de actividad).
- Sistema de notas por criptomoneda, con título, contenido, prioridad y marcas de tiempo.
- Gráficos interactivos con Chart.js: gráfico de línea, gráfico de torta y gráfico de barras.
- Cálculo de métricas estadísticas: media aritmética, desviación estándar, coeficiente de variación e interpretación de resultados.
- Comparación visual entre criptomonedas seleccionadas.
- Perfil de usuario editable con nombre para mostrar y correo electrónico.
- Preferencias de usuario, incluyendo selección del tema de la interfaz.
- Modo claro y modo oscuro, seleccionables desde preferencias y aplicados de forma consistente.
- Diseño responsive con adaptación a distintos tamaños de pantalla.

---

## Tecnologías utilizadas

- C# 12
- .NET 8
- Blazor Server
- ASP.NET Core (controladores API RESTful)
- Entity Framework Core 8 con SQL Server LocalDB
- Newtonsoft.Json para serialización
- BCrypt.Net para el hashing de contraseñas
- JavaScript (interoperabilidad Blazor) para lógica de cliente y manejo de gráficos
- Chart.js para visualizaciones
- HTML5 y CSS3 con variables CSS personalizadas
- Bootstrap 5 como framework de estilos base
- API externa de datos de criptomonedas (Coinlore API y configuración de respaldo para CoinGecko)
- Swashbuckle / Swagger para documentación de la API

---

## Arquitectura general

El proyecto sigue una organización por capas con responsabilidades claramente separadas:

- **Presentación**: componentes Razor en `Pages/` y `Shared/` que conforman la interfaz de usuario. Cada página representa una sección del panel (dashboard, criptomonedas, estadísticas, perfil, preferencias, ayuda).
- **Servicios**: clases en `Services/` que encapsulan la lógica de negocio. Incluyen autenticación, estado de sesión, servicio de API de criptomonedas, servicio de notificaciones y servicio de perfil.
- **Controladores**: controladores API en `Controllers/` que exponen endpoints RESTful para la gestión de criptomonedas, notas y perfil.
- **Modelos y acceso a datos**: entidades en `Data/` y `CryptoDbContext` con Entity Framework Core sobre SQL Server LocalDB.
- **Interoperabilidad con JavaScript**: scripts en `wwwroot/js/` para manejo de gráficos de Chart.js, selección de moneda, accesibilidad de login y alternancia de tema.
- **API externa**: consulta de datos de mercado de criptomonedas mediante `HttpClient`, configurado en `Program.cs`.

La inyección de dependencias se configura en `Program.cs`, registrando el contexto de base de datos, los servicios personalizados y el cliente HTTP para la API externa.

---

## Identidad Visual

La interfaz utiliza una estética dark dashboard inspirada en plataformas financieras y de monitoreo de criptomonedas. Combina fondos oscuros, tarjetas de información, acentos naranjas y colores semánticos para representar variaciones positivas y negativas del mercado.

Paleta de colores utilizada en el proyecto:

```css
:root {
  --bg-main: #0b1220;
  --bg-surface: #121a2b;
  --bg-card: #1a2236;
  --color-primary: #ff7a00;
  --color-secondary: #22c55e;
  --color-error: #ef4444;
  --color-warning: #f59e0b;
  --color-info: #1b77d2;
  --text-primary: #e5e7eb;
  --text-secondary: #9ca3af;
}
```

Los tres valores `--bg-*` definen los tres niveles de fondo de la aplicación: fondo general, superficies secundarias (sidebar, paneles) y tarjetas de contenido. `--color-primary` (naranja) es el acento principal de la identidad. `--color-secondary` y `--color-error` representan indicadores positivos y negativos del mercado. `--color-warning` y `--color-info` completan la paleta semántica. Los valores `--text-*` definen la jerarquía de texto sobre los fondos oscuros.

---

## Demo

Esta sección queda preparada para incluir material visual del proyecto en funcionamiento.

- Link al video demo: (https://youtu.be/8zhTYH3Q0sg)
- Probar la app: (https://cryptoview.runasp.net/login)

---

## Instalación y Ejecución

### Requisitos previos

- .NET 8 SDK instalado.
- SQL Server LocalDB disponible (incluido con Visual Studio o instalable de forma independiente).
- Visual Studio 2022 o Visual Studio Code (opcional).

### Pasos

1. Clonar el repositorio.
2. Abrir la solución `CryptoView.sln` o la carpeta del proyecto en el editor.
3. Restaurar las dependencias:

   ```sh
   dotnet restore
   ```

4. Configurar la cadena de conexión en `appsettings.json` si la instancia de LocalDB no coincide con la predeterminada.
5. Ejecutar el proyecto. La base de datos se crea de forma automática al iniciar la aplicación mediante `EnsureCreated` y migraciones de Entity Framework Core.

   ```sh
   dotnet run
   ```

6. Abrir el navegador en la URL indicada por la consola (normalmente `https://localhost:puerto`).

La documentación de la API está disponible en `/swagger` durante la ejecución en entorno de desarrollo.

Nota: la configuración de ejemplo incluida en `appsettings.json` utiliza una cadena de conexión a LocalDB de desarrollo. Reemplazar estos valores por los correspondientes al entorno de despliegue cuando corresponda.

---

## Estado del proyecto

Versión académica / portfolio. El proyecto se encuentra en estado de desarrollo finalizado para los fines educativos planteados, con las funcionalidades descritas implementadas y verificadas de forma manual. No cuenta con pruebas automatizadas ni despliegue productivo.

---

## Autor

- **Naiquen Iturralde**
- Tecnicatura Superior en Desarrollo de Software
- Portfolio / Proyecto académico
