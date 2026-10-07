# Imágenes de productos

## Contrato HTTP

Las tres rutas requieren `Authorization: Bearer <JWT>`. POST y PUT requieren un único claim autenticado `userLogin`, obtenido con el mecanismo existente de Inventory.

| Método | Ruta | Entrada | Resultado exitoso |
| --- | --- | --- | --- |
| POST | `/api/Product/{productId}/image` | `multipart/form-data`, archivo `file` | `productId`, `imagePath` |
| GET | `/api/Product/{productId}/image` | Sin cuerpo | `productId`, `imagePath`, `imageUrl`, `expiresAt` UTC |
| PUT | `/api/Product/{productId}/image` | `multipart/form-data`, archivo `file` | `productId`, `imagePath` |

Todos usan `ResponseApi`. ID/archivo inválido: 400. Claim de escritura ausente: 403. Producto inexistente o GET sin imagen: 404. POST con imagen existente: 400 indicando usar PUT. Cambio concurrente durante la escritura: 409. Error inesperado: 500 sin detalles internos. Sin JWT válido, el middleware existente devuelve 401.

Extensiones admitidas: `.jpg`, `.jpeg`, `.png`, `.webp`, ignorando mayúsculas. El Content-Type debe corresponder a la extensión: `image/jpeg`, `image/png` o `image/webp`. Archivo no vacío y hasta 5 × 1024 × 1024 bytes. Se validan los metadatos de carga; no se incorpora un decodificador ni análisis antivirus. Los límites de transporte del servidor/proxy se aplican antes del controller para cuerpos muy grandes.

## Configuración no sensible

Sección `GoogleCloudStorage` en `Inventory.Api/appsettings.json` (o las variables de entorno equivalentes):

```json
{
  "GoogleCloudStorage": {
    "BucketName": "calco-storage",
    "ProductImagePrefix": "sitracalco_inventory_dev/image/products",
    "SignedUrlExpirationMinutes": 15,
    "MaxImageSizeMb": 5
  }
}
```

El object name generado es `sitracalco_inventory_dev/image/products/{productId}/{guid:N}.{extension}`. La extensión se normaliza y el nombre original no determina la ruta. Solo ese object name se guarda en `image_path`. No se guardan el bucket, la URL firmada ni credenciales en MySQL. No se modifican ACLs ni IAM desde el código.

## Consistencia y auditoría

`ProductApplication` coordina repositorio, `IStorageService` y `ILogApplication`. GCS se usa únicamente desde `GoogleCloudStorageService`, con el paquete oficial `Google.Cloud.Storage.V1` 5.0.0 y ADC.

1. Consultar el producto. POST rechaza una imagen existente; PUT también permite cargar si aún no tiene imagen.
2. Subir el objeto nuevo con GUID y precondición GCS de no sobrescritura.
3. Actualizar únicamente `image_path`, condicionando la escritura a que conserve el valor leído. Esto detecta solicitudes concurrentes sin una nueva columna.
4. Si MySQL falla o la comparación no coincide, intentar eliminar el objeto nuevo; la imagen anterior se conserva.
5. Una vez guardada la ruta, registrar auditoría con `userLogin`. En PUT, intentar eliminar la imagen anterior solamente si está bajo el prefijo de ese producto.

Si la limpieza falla, se registra el object name para revisión manual; no se elimina la nueva imagen ni se revierte la ruta confirmada. Si la auditoría falla después de guardar la ruta, se devuelve 500 según el patrón del proyecto, pero la nueva imagen permanece guardada. No existe una transacción distribuida entre MySQL y GCS ni una cola de reintentos de limpieza en este bloque.

`ProductRepository.UpsertRange` ya modifica solamente el nombre de los productos existentes y conserva `image_path`. Su lógica no se cambió.

GET genera una Signed URL V4 usando la misma fecha de expiración que devuelve al cliente. No comprueba la existencia del objeto en GCS; una ruta cuyo objeto haya sido borrado externamente producirá una URL que devolverá 404 al abrirla. La respuesta GET se marca `no-store`.

## ADC: configuración manual posterior

No se ejecutaron comandos de GCP ni se configuraron credenciales. Para probar localmente, con Google Cloud CLI disponible y una Service Account **ya existente y autorizada**, configurar ADC mediante impersonación:

```powershell
gcloud auth application-default login --impersonate-service-account=SERVICE_ACCOUNT_EMAIL
```

Reemplazar `SERVICE_ACCOUNT_EMAIL` por la cuenta acordada con el responsable de infraestructura. El archivo ADC queda fuera del repositorio. Si `GOOGLE_APPLICATION_CREDENTIALS` apunta a otra configuración, esta tiene prioridad sobre ADC local; comprobarlo en el entorno donde inicia la API. No copiar archivos de credenciales a Inventory.

Reiniciar la API después de configurar o cambiar ADC, especialmente si ya falló una operación por credenciales ausentes.

Una sesión ADC de usuario creada solamente con `gcloud auth application-default login` puede permitir operaciones de Storage, pero no es una credencial de firma admitida por `UrlSigner.FromCredential`. Para las Signed URLs usar ADC con impersonación o la identidad de servicio compatible del recurso en GCP; no se agrega una clave privada como solución alternativa.

El responsable de infraestructura debe confirmar los permisos existentes: acceso a crear/eliminar/leer objetos en el bucket; capacidad `iam.serviceAccounts.signBlob` sobre la cuenta firmante; e IAM Service Account Credentials API habilitada. La impersonación local requiere la autorización correspondiente sobre esa cuenta, por ejemplo Service Account Token Creator. En GCP, la Service Account adjunta también necesita capacidad de firma remota. Esta implementación no concede permisos ni activa APIs.

Fuentes oficiales: [ADC local e impersonación](https://docs.cloud.google.com/docs/authentication/set-up-adc-local-dev-environment), [credenciales compatibles con UrlSigner](https://docs.cloud.google.com/dotnet/docs/reference/Google.Cloud.Storage.V1/5.0.0/Google.Cloud.Storage.V1.UrlSigner), [requisitos de firma](https://docs.cloud.google.com/storage/docs/access-control/signing-urls-with-helpers).

## Comprobación funcional manual

1. Autorizar Swagger con un JWT válido y abrir POST o PUT de imagen: deben mostrar `multipart/form-data` y únicamente el selector `file`, además del ID de ruta.
2. POST sobre un producto sin imagen: esperar 200, revisar object name y auditoría. Repetir POST: esperar 400 indicando PUT.
3. GET: esperar URL y expiración; abrir la URL antes de vencer. No almacenar esa URL como ruta definitiva.
4. PUT: esperar una ruta diferente, comprobar la nueva imagen y eliminación de la anterior.
5. Ejecutar la sincronización cuando corresponda y confirmar que conserva la imagen.
6. Revisar los casos 400/401/403/404 y un archivo superior a 5 MB. Las pruebas de fallo de MySQL/GCS deben hacerse en un entorno de prueba controlado.

Las pruebas unitarias nuevas simulan Application y cubren únicamente responsabilidades de `ProductController`; no acceden a GCS ni MySQL.

## Archivos de este cambio

Creados:

- `Inventory.Domain/Dtos/ProductImageDto.cs`
- `Inventory.Domain/Dtos/ProductImageResultDto.cs`
- `Inventory.Domain/Helpers/ProductImageFiles.cs`
- `Inventory.Domain/Options/GoogleCloudStorageOptions.cs`
- `Inventory.Domain/Interfaces/IStorageService.cs`
- `Inventory.Infrastructure.Persistance/Services/GoogleCloudStorageService.cs`
- `docs/product-images.md`

Modificados:

- `Inventory.Api/Controllers/ProductController.cs`
- `Inventory.Api/Program.cs`
- `Inventory.Api/appsettings.json`: sección no sensible; este archivo ya está excluido por las reglas Git del repositorio. La configuración también queda documentada arriba para otros entornos.
- `Inventory.Application/Interfaces/IProductApplication.cs`
- `Inventory.Application/Services/ProductApplication.cs`
- `Inventory.Domain/Interfaces/IProductRepository.cs`
- `Inventory.Infrastructure.Persistance/Repositories/ProductRepository.cs`
- `Inventory.Infrastructure.Persistance/Data/EFExtensions.cs`
- `Inventory.Infrastructure.Persistance/Inventory.Infrastructure.Persistance.csproj`
- `Inventory.Test/Controllers/ProductControllerTests.cs`: 41 casos nuevos.
- `Inventory.Test/Controllers/AuthenticatedClaimsTests.cs`: únicamente adaptación del constructor existente para inyectar Options; sin pruebas nuevas en ese archivo.

Validación realizada: solución completa en Release (0 errores, 0 advertencias), ProductControllerTests 68/68, Inventory.Test 444/444. Swagger generado confirma `multipart/form-data` y `file` binario para POST y PUT. Peticiones HTTP sin token con Content-Type correspondiente devuelven 401 en los tres endpoints. Se cerró la instancia temporal de verificación. No se hicieron cargas ni llamadas reales a MySQL/GCS.
