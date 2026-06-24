# 🪙 CryptoView Dashboard

> **Panel de control interactivo para gestión y análisis de criptomonedas**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://blazor.net/)
[![Build](https://img.shields.io/badge/Build-Passing-success)](.)
[![License](https://img.shields.io/badge/License-Educational-blue)](.)

---

## 🎯 Descripción

**CryptoView Dashboard** es una aplicación web completa desarrollada con **Blazor Server** y **.NET 8** que permite gestionar criptomonedas favoritas, visualizar precios en tiempo real desde la API de CoinGecko, y realizar análisis estadísticos con gráficos interactivos.

**Trabajo Final Integrador** - Tecnicatura Superior en Desarrollo de Software

---

## ✨ Características Principales

- ✅ **CRUD Completo**: Crea, lee, actualiza y elimina criptomonedas
- 📊 **Dashboard en Tiempo Real**: Visualiza precios actualizados de CoinGecko API
- 📈 **Gráficos Interactivos**: Chart.js para análisis visual (línea, torta, barras)
- 🧮 **Análisis Estadístico**: Media, desviación estándar, coeficiente de variación
- 📝 **Sistema de Notas**: Agrega comentarios personalizados por moneda
- 🌙 **Tema Oscuro Profesional**: Interfaz moderna con colores vibrantes
- 🔄 **Actualización Masiva**: Refresca todos los precios con un clic
- 🔍 **Búsqueda en Tiempo Real**: Filtra criptomonedas instantáneamente
- 📡 **API RESTful**: Endpoints documentados con Swagger
- 🗄️ **Base de Datos**: SQL Server LocalDB con Entity Framework Core

---

## 🚀 Inicio Rápido

### Prerrequisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) o [VS Code](https://code.visualstudio.com/)
- SQL Server LocalDB (incluido con Visual Studio)

### Instalación

1. **Clona o descarga el proyecto**

2. **Restaura los paquetes NuGet**

   ```powershell
   cd CryptoView
   dotnet restore
   ```

3. **Ejecuta la aplicación**

   ```powershell
   dotnet run
   ```

4. **Abre tu navegador**
   ```
   https://localhost:7xxx
   ```

### Usando Visual Studio

1. Abre `CryptoView.sln`
2. Presiona **F5** o haz clic en **Run**
3. ¡Listo! La aplicación se abrirá automáticamente

---

## 📸 Capturas de Pantalla

### Dashboard Principal

- Vista general con tarjetas de criptomonedas
- Estadísticas de mercado en tiempo real
- Indicadores de cambio de precio con colores

### Gestión de Criptomonedas

- Tabla completa con búsqueda
- Modales para agregar/editar
- Sistema de notas integrado

### Análisis Estadístico

- Gráfico de línea: Crecimiento exponencial de Bitcoin
- Gráfico de torta: Distribución de Market Cap
- Gráfico de barras: Cambios de precio 24h
- Panel de métricas estadísticas

---

## 🛠️ Stack Tecnológico

### Backend

- **.NET 8** - Framework principal
- **C# 12.0** - Lenguaje de programación
- **Entity Framework Core 8** - ORM
- **ASP.NET Core** - API RESTful
- **SQL Server LocalDB** - Base de datos

### Frontend

- **Blazor Server** - Framework de UI
- **Bootstrap 5.3.2** - CSS Framework
- **Chart.js 4.4.0** - Gráficos interactivos
- **JavaScript Interop** - Integración con librerías JS

### Integraciones

- **CoinGecko API v3** - Datos de criptomonedas en tiempo real
- **Swagger/OpenAPI** - Documentación de API
- **Newtonsoft.Json** - Serialización JSON

---

## 📁 Estructura del Proyecto

```
CryptoView/
├── Data/                           # Modelos y contexto de base de datos
│   ├── CryptoCurrency.cs          # Entidad principal
│   ├── UserNote.cs                # Entidad de notas
│   └── CryptoDbContext.cs         # DbContext de EF Core
│
├── Services/                       # Servicios de negocio
│   └── CryptoApiService.cs        # Servicio de integración con CoinGecko
│
├── Controllers/                    # API RESTful
│   ├── CryptoController.cs        # CRUD de criptomonedas
│   └── NotesController.cs         # CRUD de notas
│
├── Pages/                          # Componentes Blazor
│   ├── Index.razor                # Dashboard principal
│   ├── CryptoList.razor           # Gestión CRUD
│   ├── Stats.razor                # Análisis estadístico
│   ├── _Host.cshtml               # Host page con scripts
│   └── _Imports.razor             # Imports globales
│
├── Shared/                         # Componentes compartidos
│   ├── MainLayout.razor           # Layout principal
│   └── NavMenu.razor              # Menú de navegación
│
├── wwwroot/                        # Archivos estáticos
│   └── css/
│       └── site.css               # Estilos personalizados
│
├── Program.cs                      # Configuración de la app
├── appsettings.json               # Configuración
└── CryptoView.csproj              # Archivo de proyecto
```

---

## 📡 API Endpoints

### CryptoController (`/api/Crypto`)

| Método   | Endpoint                     | Descripción                            |
| -------- | ---------------------------- | -------------------------------------- |
| `GET`    | `/api/Crypto`                | Lista todas las criptomonedas activas  |
| `GET`    | `/api/Crypto/{id}`           | Obtiene una criptomoneda específica    |
| `POST`   | `/api/Crypto`                | Crea una nueva criptomoneda            |
| `PUT`    | `/api/Crypto/{id}`           | Actualiza una criptomoneda             |
| `DELETE` | `/api/Crypto/{id}`           | Elimina una criptomoneda (soft delete) |
| `GET`    | `/api/Crypto/{id}/refresh`   | Actualiza el precio de una moneda      |
| `GET`    | `/api/Crypto/refresh-all`    | Actualiza todos los precios            |
| `GET`    | `/api/Crypto/search/{query}` | Busca criptomonedas por nombre         |

### NotesController (`/api/Notes`)

| Método   | Endpoint                       | Descripción                 |
| -------- | ------------------------------ | --------------------------- |
| `GET`    | `/api/Notes/crypto/{cryptoId}` | Obtiene notas de una moneda |
| `GET`    | `/api/Notes/{id}`              | Obtiene una nota específica |
| `POST`   | `/api/Notes`                   | Crea una nueva nota         |
| `PUT`    | `/api/Notes/{id}`              | Actualiza una nota          |
| `DELETE` | `/api/Notes/{id}`              | Elimina una nota            |

**Documentación completa**: `https://localhost:7xxx/swagger`

---

## 🗄️ Modelo de Datos

### CryptoCurrency

```csharp
public class CryptoCurrency
{
    public int Id { get; set; }                     // Clave primaria
    public string CoinId { get; set; }              // ID de CoinGecko (bitcoin, ethereum)
    public string Name { get; set; }                // Nombre completo
    public string Symbol { get; set; }              // Símbolo (BTC, ETH)
    public decimal CurrentPrice { get; set; }       // Precio actual en USD
    public decimal MarketCap { get; set; }          // Capitalización de mercado
    public decimal Volume24h { get; set; }          // Volumen 24h
    public decimal Change24h { get; set; }          // Cambio % 24h
    public decimal? PriceThreshold { get; set; }    // Umbral de alerta (opcional)
    public DateTime LastUpdated { get; set; }       // Última actualización
    public DateTime DateAdded { get; set; }         // Fecha de agregado
    public bool IsActive { get; set; }              // Soft delete
    public ICollection<UserNote> Notes { get; set; } // Relación 1:N con notas
}
```

### UserNote

```csharp
public class UserNote
{
    public int Id { get; set; }                     // Clave primaria
    public int CryptoCurrencyId { get; set; }       // Clave foránea
    public string Title { get; set; }               // Título de la nota
    public string Content { get; set; }             // Contenido
    public DateTime CreatedAt { get; set; }         // Fecha de creación
    public DateTime UpdatedAt { get; set; }         // Última modificación
    public int Priority { get; set; }               // 1=Baja, 2=Media, 3=Alta
    public CryptoCurrency CryptoCurrency { get; set; } // Navegación
}
```

**Relación**: Una criptomoneda puede tener muchas notas (1:N con cascade delete)

---

## 📐 Análisis Estadístico

### Conceptos Implementados

#### 1. Media Aritmética (μ)

```
μ = Σ(xi) / n
```

Calcula el precio promedio de Bitcoin en el período analizado.

#### 2. Desviación Estándar (σ)

```
σ = √[Σ(xi - μ)² / n]
```

Mide la volatilidad del precio. Mayor σ = mayor volatilidad.

#### 3. Coeficiente de Variación (CV)

```
CV = (σ / μ) × 100%
```

Permite comparar volatilidad relativa entre diferentes activos.

#### 4. Modelo Exponencial de Crecimiento

```
P(t) = P₀ · e^(rt)
```

- **P(t)**: Precio en el tiempo t
- **P₀**: Precio inicial ($1,000 USD)
- **r**: Tasa de crecimiento (0.003)
- **t**: Tiempo en días

Simula el crecimiento histórico de Bitcoin desde 2010.

---

## 🎨 Identidad Visual

### Paleta de Colores (Tema Oscuro)

| Color               | Hex       | Uso                     |
| ------------------- | --------- | ----------------------- |
| **Negro Profundo**  | `#0D1117` | Fondo principal         |
| **Naranja**         | `#ff8c00` | Color primario, botones |
| **Verde Neón**      | `#39ff14` | Indicadores positivos   |
| **Azul Eléctrico**  | `#7df9ff` | Enlaces, hover          |
| **Amarillo Dorado** | `#ffdf00` | Texto destacado         |
| **Rojo**            | `#ff4444` | Indicadores negativos   |

### CSS Variables

```css
:root {
  --bg-primary: #0d1117;
  --bg-secondary: #161b22;
  --color-primary: #ff8c00;
  --color-secondary: #39ff14;
  --color-blue: #7df9ff;
  --color-yellow: #ffdf00;
  --text-primary: #c9d1d9;
  --text-secondary: #8b949e;
}
```

---

## 🏗️ Arquitectura

### Patrón en Capas

```
┌─────────────────────────────────────┐
│    Presentación (Blazor Pages)      │  ← Razor Components
├─────────────────────────────────────┤
│    API (Controllers)                │  ← RESTful Endpoints
├─────────────────────────────────────┤
│    Servicios (Business Logic)       │  ← CryptoApiService
├─────────────────────────────────────┤
│    Acceso a Datos (EF Core)         │  ← DbContext, Models
├─────────────────────────────────────┤
│    Base de Datos (SQL Server)       │  ← CryptoViewDb
└─────────────────────────────────────┘
```

### Inyección de Dependencias

```csharp
// Program.cs
builder.Services.AddDbContext<CryptoDbContext>();
builder.Services.AddScoped<CryptoApiService>();
builder.Services.AddHttpClient();
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSwaggerGen();
```

---

## 🧪 Testing

### Verificación Manual

1. **CRUD de Criptomonedas**
   - ✅ Agregar Bitcoin, Ethereum, Cardano
   - ✅ Editar umbral de precio
   - ✅ Eliminar (soft delete)
   - ✅ Búsqueda por nombre/símbolo

2. **Actualización de Precios**
   - ✅ Actualizar precio individual
   - ✅ Actualizar todos los precios
   - ✅ Verificar timestamp de actualización

3. **Sistema de Notas**
   - ✅ Agregar nota con prioridad
   - ✅ Editar contenido
   - ✅ Eliminar nota
   - ✅ Verificar timestamps

4. **Visualizaciones**
   - ✅ Gráfico de línea renderiza correctamente
   - ✅ Gráfico de torta muestra distribución
   - ✅ Gráfico de barras con colores dinámicos
   - ✅ Métricas estadísticas calculan correctamente

---

## 🔧 Configuración

### Connection String

Edita `appsettings.json` para cambiar la base de datos:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CryptoViewDb;Trusted_Connection=True"
  }
}
```

### Logging

Configurado en `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 🐛 Solución de Problemas

### No se puede conectar a LocalDB

**Error**: `Cannot connect to (localdb)\mssqllocaldb`

**Solución**:

```powershell
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

### CoinGecko API retorna 429

**Error**: `Too Many Requests`

**Solución**: Espera 1-2 minutos entre actualizaciones masivas. La API gratuita tiene rate limiting.

### Chart.js no carga

**Error**: Gráficos no se renderizan

**Solución**: Verifica que el CDN de Chart.js esté accesible en `_Host.cshtml`:

```html
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0"></script>
```

---

## 📚 Documentación Adicional

- 📖 **Guía Completa**: Ver `guiaPasoAPaso.md` en la raíz del workspace
- 🌐 **Swagger UI**: `https://localhost:7xxx/swagger`
- 💻 **Código Comentado**: Todos los archivos tienen comentarios explicativos

---

## 🎓 Requisitos Académicos Cumplidos

### Programación II ✅

- ✅ Programación Orientada a Objetos (C#)
- ✅ CRUD completo con Entity Framework Core
- ✅ API RESTful con ASP.NET Core
- ✅ Manejo de excepciones y logging
- ✅ Inyección de dependencias
- ✅ Patrones de diseño (Repository, Service)

### Probabilidad y Estadística ✅

- ✅ Cálculo de media y desviación estándar
- ✅ Coeficiente de variación
- ✅ Visualización de distribuciones
- ✅ Modelo de crecimiento exponencial
- ✅ Interpretación estadística de resultados

### Diseño de Sistemas ✅

- ✅ Arquitectura en capas
- ✅ Separación de responsabilidades
- ✅ Diagrama de entidades implementado
- ✅ Documentación completa
- ✅ Código escalable y mantenible

---

## 🚀 Roadmap (Mejoras Futuras)

- [ ] Autenticación con ASP.NET Core Identity
- [ ] Sistema de notificaciones (Email/WhatsApp)
- [ ] Exportación a PDF/CSV/Excel
- [ ] Integración con NewsAPI para noticias
- [ ] Calculadora de inversión (ROI)
- [ ] Modo claro/oscuro toggle
- [ ] Tests unitarios con xUnit
- [ ] Deploy en Azure

---

## 📝 Licencia

Este proyecto fue desarrollado con **fines educativos** como parte del Trabajo Final Integrador de la Tecnicatura Superior en Desarrollo de Software.

**Año**: 2025

---

## 📞 Contacto y Soporte

Para dudas sobre el proyecto:

1. Consulta la documentación en `guiaPasoAPaso.md`
2. Revisa los comentarios en el código fuente
3. Accede a la documentación Swagger
4. Consulta los logs de la aplicación

---

## 🙏 Recursos y Referencias

- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [CoinGecko API Docs](https://www.coingecko.com/en/api/documentation)
- [Chart.js Documentation](https://www.chartjs.org/docs/latest/)
- [Bootstrap 5](https://getbootstrap.com/docs/5.3/)
- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/)

---

## ✅ Estado del Proyecto

```
✅ Build: Passing
✅ Compilation: Success
✅ Tests: Manual Testing Required
✅ Documentation: Complete
✅ Features: 100% Implemented
```

**Última actualización**: 2025

---

<div align="center">

**Desarrollado con ❤️ para el Trabajo Final Integrador**

🪙 **CryptoView Dashboard** 🚀

[Documentación](../guiaPasoAPaso.md) • [API Docs](/swagger)

</div>
