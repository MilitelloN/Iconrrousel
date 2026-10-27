## 3) Pantalla de configuracion:

-- Cambiar la orientacion de horitonzal a vertical. La expansion actua acorde a la orientacion.
-- En la configuracion se puede decidir si se ancla arriba o abajo (OH) o derecha/izquierda (OV).

## Menu contextual del icono (Boton derecho)

-- Hay juegos que su exe no tiene el mismo nombre, se debe poder cambiar el nombre que se muestra haciendo click derecho sobre el icono/ cambiar nombre.
-- Se podran crear mensajes que seran mostrados antes de iniciar el juego a modo recordatorio. Esta ventana debe tener un checkbox para determinar si se sigue mostrando o se oculta.
Se debe registrar ultima vez iniciado el juego, si paso mas de una semana, se muestra el mensaje, click en ok no lo muestra mas hasta que pase una semana desde la ultima vez ejecutado.
El mensaje sera un modal que puede tener tamano variable
Se debe poder elegir una frecuencia de cada cuanto mostrar el mensaje: Cada vez que se inicia el juego, cada X dias
-- Boton derecho sobre los iconos, se agrega: - Cambiar path del exe - Cambiar nombre - Cambiar plataforma. - Agregar mensaje - Abrir ubicacion del exe

# SOLUCIONADO
## 1) Actualizar runtime: Net framework 4.8 a NET CORE

## 2) Correcciones
-- Se detecto un bug cuando se usa en una pantalla que no tiene la misma resolucion: No se centra bien en la pantalla y queda desplazado.
	**** DEBERIA ESTAR SOLUCIONADO.

## 3) FUNCIONALIDADES:
-- Se reconoce la plataforma que instala el juego y el fondo del icono se pinta del color del mismo. 
-- Tambien se implementaron los filtros por plataforma.
-- El tema de la aplicacion ahora cambia con el color personalizado.