# 🪚 Sistema de Gestión de Carpintería  

📌 **SistemaGestionI** es una aplicación diseñada para optimizar y digitalizar la gestión integral de una carpintería.  
El objetivo principal es brindar una herramienta intuitiva y eficiente que permita administrar clientes, mayoristas, materiales, stock, pedidos, pagos y facturación en un solo sistema centralizado.  

---

## 🚀 Objetivo del Proyecto  
El sistema busca:  
- Mejorar la **organización de clientes y proveedores**.  
- Controlar de forma precisa el **stock de materiales e insumos**.  
- Gestionar **turnos, pedidos y pagos** de manera centralizada.  
- Generar **presupuestos y facturación simulada** para clientes.  
- Proporcionar un **entorno seguro, accesible y multiplataforma**.  

---

## ⚙️ Requerimientos Funcionales  

### 👥 Gestión de Clientes  
- Registrar, editar y eliminar clientes.  
- Buscar por nombre, apellido o teléfono.  

### 🏢 Gestión de Mayoristas  
- Registrar mayoristas (melaminas, insumos, accesorios).  
- Asociar mayoristas a un tipo.  
- Consultar contactos registrados.  

### 📦 Gestión de Materiales y Stock  
- Registrar modelos de melaminas con medidas y stock.  
- Registrar insumos y accesorios.  
- Modificar stock en tiempo real.  
- Alertas automáticas de stock bajo.    

### 📝 Gestión de Pedidos  
- Registrar pedidos con uno o varios materiales.  
- Asignar estados (pendiente, en proceso, entregado).  
- Consultar pedidos por cliente o estado.  

### 💰 Gestión de Pagos  
- Registrar pagos vinculados a pedidos.  
- Clasificación en seña, pago total o saldo.  
- Historial de pagos por cliente.  

### 📑 Facturación y Presupuestos  
- Generar presupuestos con validez de 15 días.  
- Asociar presupuestos a clientes.  
- Emitir facturas simuladas.  

### 👨‍💼 Información del Administrador  
- Registrar datos de contacto del administrador.  
- Hacer visibles estos datos para los clientes.  

---

## 🛡️ Requerimientos No Funcionales  

### 🔒 Seguridad  
- Acceso solo para usuarios autenticados.  
- Contraseñas cifradas (hash).  
- Control de permisos para modificación y eliminación.  
- Transmisión segura mediante HTTPS.  

### ⚡ Rendimiento  
- Respuestas a consultas en menos de 2 segundos.  
- Manejo de al menos 50 usuarios concurrentes.  

### 📂 Disponibilidad y Confiabilidad  
- Disponibilidad del **95% en horario laboral**.  
- Registro de errores críticos en logs.  
- Respaldo automático diario de la base de datos.  

### 🖥️ Usabilidad  
- Interfaz intuitiva y fácil de usar.  
- Búsquedas rápidas y eficientes.  
- Compatible con PC y dispositivos móviles.  

### 🔄 Mantenibilidad y Escalabilidad  
- Arquitectura modular.  
- Código documentado y versionado (GitHub/GitLab).  
- Escalabilidad en almacenamiento sin afectar el sistema.  

### 🌐 Compatibilidad  
- Compatible con **Visual Studio 2022 y .NET 6+**.  
- Base de datos en **SQL Server**.  
- Exportación a **PDF, Excel y CSV**.  

---

## 🛠️ Tecnologías Recomendadas  
- **Backend:** .NET 8 
- **Base de datos:** HeidiSQL
- **IDE:** Visual Studio 2022  
- **Frontend:** Blazor / ASP.NET MVC / Razor Pages (según la arquitectura elegida)  

---

## 📌 Estado del Proyecto  
📍 *En desarrollo inicial* – Se están implementando los módulos de gestión de clientes y mayoristas.  

---

## 🤝 Contribuciones  
Las contribuciones son bienvenidas.  
Si deseas colaborar:  
1. Haz un **fork** del repositorio.  
2. Crea una rama con tu feature (`git checkout -b feature/nueva-funcionalidad`).  
3. Realiza un commit (`git commit -m 'Agrego nueva funcionalidad'`).  
4. Haz un push (`git push origin feature/nueva-funcionalidad`).  
5. Abre un **Pull Request**.  

---

## 📧 Contacto  
👨‍💻 **Administrador del Proyecto**  
- ✉️ Email: [correo@ejemplo.com](mailto:correo@ejemplo.com)  
- 📱 Teléfono: +54 9 1234 567890  
- 🌐 Redes sociales: [Facebook](#) | [Instagram](#) | [LinkedIn](#)  

---
✨ *SistemaGestionI – Una solución digital para llevar tu carpintería al siguiente nivel.*  
