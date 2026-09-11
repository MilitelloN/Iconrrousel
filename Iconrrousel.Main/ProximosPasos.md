# Pantalla de configuracion:

-- La pantalla de configuracion tiene que tener un path (detectado o no) hacia las carpetas de juegos y clasificar el juego por su plataforma
-- Orientacion horitonzal o vertical. La expancion actua acorde a la orientacion.
-- En la configuracion se puede decidir si se ancla arriba o abajo (OH) o derecha/izquierda (OV).

# Menu contextual del icono (Boton derecho)

-- Hay juegos que su exe no tiene el mismo nombre, se debe poder cambiar el nombre que se muestra haciendo click derecho sobre el icono/ cambiar nombre.
-- Se podran crear mensajes que seran mostrados antes de iniciar el juego a modo recordatorio. Esta ventana debe tener un checkbox para determinar si se sigue mostrando o se oculta.
Se debe registrar ultima vez iniciado el juego, si paso mas de una semana, se muestra el mensaje, click en ok no lo muestra mas hasta que pase una semana desde la ultima vez ejecutado.
El mensaje sera un modal que puede tener tamano variable
Se debe poder elegir una frecuencia de cada cuanto mostrar el mensaje: Cada vez que se inicia el juego, cada X dias
-- Boton derecho sobre los iconos, se agrega: - Cambiar path del exe - Cambiar nombre - Cambiar plataforma. - Agregar mensaje - Abrir ubicacion del exe

# Nuevas caracteristicas

-- A partir de la deteccion de plataforma, se debe obtener el exe desde esa carpeta evitando que se use las del escritorio, de esta forma si se elimina el icono del escritorio, el exe sigue funcionando.
-- El fondo del icono, cuando se pasa el mouse por encima, debe pintarse del color de la plataforma (color semi transparente):
Steam: Azul
Epic: Gris oscuro
Xbox: Verde
Gog: Violeta
EA: Naranja
-- Boton de filtros: - Filtrar por plataforma - Mostrar ultimos 5 jugados

# Correcciones

-- Se detecto un bug en la notebook: No se centra bien en la pantalla y queda desplazado.
