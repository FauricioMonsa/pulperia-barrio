-- =====================================================================
-- PRUEBAS 
-- =====================================================================
SET search_path TO user_08;

-- ---------- ÉXITO ----------
-- T1. Ingreso de inventario suma al disponible.
-- Esperado: disponible de suc 1 / prod 3 pasa de 20 a 25.
INSERT INTO ingreso_inventario (sucursal_id, producto_id, cantidad) VALUES (1,3,5);
SELECT cantidad_disponible FROM inventario WHERE sucursal_id=1 AND producto_id=3;

-- T2. Reserva válida descuenta inventario y calcula vencimiento (48 h zapatos).
-- Esperado: inserta, estado RESERVADO, disponible baja 1.
INSERT INTO reserva (cliente_id, sucursal_id, producto_id, cantidad, fecha_vencimiento)
VALUES (3,1,1,1, now());
SELECT * FROM vw_reservas_clientes ORDER BY reserva_id DESC LIMIT 1;

-- T3. Cancelar una reserva RESERVADO devuelve las unidades al inventario.
-- Esperado: estado CANCELADO y disponible vuelve a subir.
UPDATE reserva SET estado = 'CANCELADO' WHERE id = 1;
SELECT * FROM vw_productos_inventario WHERE sucursal_id=1 AND producto_id=1;

-- ---------- ERROR ----------
-- E1. UPDATE de ingreso_inventario. Esperado: ERROR "solo inserción".
UPDATE ingreso_inventario SET cantidad = 999 WHERE id = 1;

-- E2. DELETE de ingreso_inventario. Esperado: ERROR.
DELETE FROM ingreso_inventario WHERE id = 1;

-- E3. TRUNCATE de ingreso_inventario. Esperado: ERROR.
TRUNCATE ingreso_inventario;

-- E4. Ingreso con cantidad <= 0. Esperado: ERROR CHECK (cantidad > 0).
INSERT INTO ingreso_inventario (sucursal_id, producto_id, cantidad) VALUES (1,1,0);

-- E5. Reservar más de lo disponible. Zapatos suc 4: disponible 4, máx 3 por cliente.
-- Una sola instrucción con dos filas: la 2ª pide 3 cuando solo queda 1.
-- Esperado: ERROR "No hay suficiente inventario disponible (disponible: 1)".
-- Todo se revierte, no queda ninguna reserva.
INSERT INTO reserva (cliente_id, sucursal_id, producto_id, cantidad, fecha_vencimiento)
VALUES (1,4,1,3, now()), (2,4,1,3, now());

-- E6. Exceder máximo por cliente (crocs máx 1, se piden 2). Esperado: ERROR "Excede el máximo por cliente (1)".
INSERT INTO reserva (cliente_id, sucursal_id, producto_id, cantidad, fecha_vencimiento)
VALUES (4,3,2,2, now());

-- E7. Violar existencia mínima. Laptop suc 4: disponible 3. Subimos el mínimo a 3.
-- Ejecutar las 3 instrucciones por separado y en orden.
-- Esperado: la 2ª da ERROR "La reserva violaría la existencia mínima (3)".
UPDATE inventario SET existencia_minima = 3 WHERE sucursal_id = 4 AND producto_id = 4;
INSERT INTO reserva (cliente_id, sucursal_id, producto_id, cantidad, fecha_vencimiento)
VALUES (5,4,4,1, now());
UPDATE inventario SET existencia_minima = 1 WHERE sucursal_id = 4 AND producto_id = 4;  -- restaurar

-- E8. Modificar un campo distinto al estado. Esperado: ERROR "Solo se permite actualizar el campo estado".
UPDATE reserva SET cantidad = 1 WHERE id = 3;

-- E9. Modificar una reserva CANCELADA/COMPRADA. Esperado: ERROR "ya no se puede modificar".
UPDATE reserva SET estado = 'RESERVADO' WHERE id = 5;
UPDATE reserva SET estado = 'CANCELADO' WHERE id = 2;

-- E10. DELETE y TRUNCATE de reserva. Esperado: ERROR en ambos.
DELETE FROM reserva WHERE id = 3;
TRUNCATE reserva;

-- E11. Estado inválido. Esperado: ERROR del trigger "Desde RESERVADO solo se puede pasar a COMPRADO o CANCELADO".
UPDATE reserva SET estado = 'XYZ' WHERE id = 3;

-- E12. FK inexistente (cliente 999). Esperado: ERROR violación de llave foránea.
INSERT INTO reserva (cliente_id, sucursal_id, producto_id, cantidad, fecha_vencimiento)
VALUES (999,1,1,1, now());

-- E13. Correo de cliente duplicado. Esperado: ERROR UNIQUE.
INSERT INTO cliente (nombre, correo) VALUES ('Repetido', 'maria.fernandez@mail.com');

-- E14. Atributos JSON que no es objeto. Esperado: ERROR CHECK jsonb.
INSERT INTO producto (nombre, categoria, precio, max_cantidad_reserva, max_horas_reserva, atributos)
VALUES ('Malo','Prueba',100,1,1,'[1,2,3]');

-- E15. Cliente penalizado. Esperado: ERROR "Cliente penalizado hasta ...".
UPDATE cliente SET baneado_hasta = now() + interval '1 day' WHERE id = 5;
INSERT INTO reserva (cliente_id, sucursal_id, producto_id, cantidad, fecha_vencimiento)
VALUES (5,2,5,1, now());
UPDATE cliente SET baneado_hasta = NULL WHERE id = 5;   -- limpiar


