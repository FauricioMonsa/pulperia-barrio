-- =====================================================================
-- Mi Pulpería de Barrio - Entregable 1
-- Base: advanced_programming (PostgreSQL)
-- Nombre: Fauricio Montoya Salas
-- =====================================================================

SET search_path TO user_08;

-- Si necesitas volver a correr el script desde cero, ejecuta ANTES este bloque
-- (descomentado). Solo borra los objetos de este proyecto:
-- DROP VIEW IF EXISTS vw_reservas_clientes, vw_productos_inventario;
-- DROP TABLE IF EXISTS reserva, ingreso_inventario, inventario, producto, cliente, sucursal CASCADE;
-- DROP FUNCTION IF EXISTS fn_ingreso_after_insert(), fn_bloquear_modificacion(),
--   fn_reserva_before_insert(), fn_reserva_before_update(), fn_cancelar_reservas_vencidas();

-- ---------------------------------------------------------------------
-- TABLAS
-- ---------------------------------------------------------------------
CREATE TABLE sucursal (
    id         INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre     VARCHAR(100) NOT NULL UNIQUE,
    ubicacion  VARCHAR(200) NOT NULL,
    activa     BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE cliente (
    id             INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre         VARCHAR(120) NOT NULL,
    correo         VARCHAR(150) NOT NULL UNIQUE,
    telefono       VARCHAR(20),
    baneado_hasta  TIMESTAMPTZ            -- TODO del profe: penalización (Polly)
);

CREATE TABLE producto (
    id                     INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre                 VARCHAR(120) NOT NULL UNIQUE,
    categoria              VARCHAR(60)  NOT NULL,
    precio                 NUMERIC(12,2) NOT NULL CHECK (precio > 0),
    max_cantidad_reserva   INT NOT NULL CHECK (max_cantidad_reserva > 0),
    max_horas_reserva      INT NOT NULL CHECK (max_horas_reserva > 0),
    atributos              JSONB NOT NULL DEFAULT '{}'::jsonb
                           CHECK (jsonb_typeof(atributos) = 'object')
);

CREATE TABLE inventario (
    sucursal_id         INT NOT NULL REFERENCES sucursal(id),
    producto_id         INT NOT NULL REFERENCES producto(id),
    cantidad_disponible INT NOT NULL DEFAULT 0 CHECK (cantidad_disponible >= 0),
    existencia_minima   INT NOT NULL DEFAULT 0 CHECK (existencia_minima >= 0),  -- TODO del profe
    PRIMARY KEY (sucursal_id, producto_id)
);

CREATE TABLE ingreso_inventario (
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sucursal_id INT NOT NULL REFERENCES sucursal(id),
    producto_id INT NOT NULL REFERENCES producto(id),
    cantidad    INT NOT NULL CHECK (cantidad > 0),
    fecha       TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE reserva (
    id                INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cliente_id        INT NOT NULL REFERENCES cliente(id),
    sucursal_id       INT NOT NULL REFERENCES sucursal(id),
    producto_id       INT NOT NULL REFERENCES producto(id),
    cantidad          INT NOT NULL CHECK (cantidad > 0),
    fecha_reserva     TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_vencimiento TIMESTAMPTZ NOT NULL,
    estado            VARCHAR(10) NOT NULL DEFAULT 'RESERVADO'
                      CHECK (estado IN ('RESERVADO','COMPRADO','CANCELADO')),
    FOREIGN KEY (sucursal_id, producto_id) REFERENCES inventario(sucursal_id, producto_id)
);

CREATE INDEX ix_reserva_cliente ON reserva(cliente_id, fecha_reserva DESC);
CREATE INDEX ix_reserva_estado  ON reserva(estado, fecha_vencimiento);

-- ---------------------------------------------------------------------
-- TRIGGERS: ingreso_inventario (solo INSERT)
-- ---------------------------------------------------------------------
CREATE FUNCTION fn_ingreso_after_insert() RETURNS trigger AS $$
BEGIN
    INSERT INTO inventario (sucursal_id, producto_id, cantidad_disponible)
    VALUES (NEW.sucursal_id, NEW.producto_id, NEW.cantidad)
    ON CONFLICT (sucursal_id, producto_id)
    DO UPDATE SET cantidad_disponible = inventario.cantidad_disponible + EXCLUDED.cantidad_disponible;
    RETURN NEW;
END $$ LANGUAGE plpgsql;

CREATE TRIGGER trg_ingreso_after_insert
AFTER INSERT ON ingreso_inventario
FOR EACH ROW EXECUTE FUNCTION fn_ingreso_after_insert();

CREATE FUNCTION fn_bloquear_modificacion() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'Operación % no permitida sobre %: los registros son de solo inserción (auditoría)',
        TG_OP, TG_TABLE_NAME;
END $$ LANGUAGE plpgsql;

CREATE TRIGGER trg_ingreso_no_update_delete
BEFORE UPDATE OR DELETE ON ingreso_inventario
FOR EACH ROW EXECUTE FUNCTION fn_bloquear_modificacion();

CREATE TRIGGER trg_ingreso_no_truncate
BEFORE TRUNCATE ON ingreso_inventario
FOR EACH STATEMENT EXECUTE FUNCTION fn_bloquear_modificacion();

-- ---------------------------------------------------------------------
-- TRIGGERS: reserva
-- ---------------------------------------------------------------------
CREATE FUNCTION fn_reserva_before_insert() RETURNS trigger AS $$
DECLARE
    v_prod       producto%ROWTYPE;
    v_inv        inventario%ROWTYPE;
    v_baneado    TIMESTAMPTZ;
    v_ya_reserva INT;
BEGIN
    SELECT baneado_hasta INTO v_baneado FROM cliente WHERE id = NEW.cliente_id;
    IF v_baneado IS NOT NULL AND v_baneado > now() THEN
        RAISE EXCEPTION 'Cliente penalizado hasta %', v_baneado;
    END IF;

    SELECT * INTO v_prod FROM producto WHERE id = NEW.producto_id;

    SELECT * INTO v_inv FROM inventario
    WHERE sucursal_id = NEW.sucursal_id AND producto_id = NEW.producto_id
    FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'El producto % no existe en la sucursal %', NEW.producto_id, NEW.sucursal_id;
    END IF;

    SELECT COALESCE(SUM(cantidad), 0) INTO v_ya_reserva
    FROM reserva
    WHERE cliente_id = NEW.cliente_id AND producto_id = NEW.producto_id AND estado = 'RESERVADO';

    IF v_ya_reserva + NEW.cantidad > v_prod.max_cantidad_reserva THEN
        RAISE EXCEPTION 'Excede el máximo por cliente (%) para "%"',
            v_prod.max_cantidad_reserva, v_prod.nombre;
    END IF;

    IF NEW.cantidad > v_inv.cantidad_disponible THEN
        RAISE EXCEPTION 'No hay suficiente inventario disponible (disponible: %)', v_inv.cantidad_disponible;
    END IF;

    IF v_inv.cantidad_disponible - NEW.cantidad < v_inv.existencia_minima THEN
        RAISE EXCEPTION 'La reserva violaría la existencia mínima (%) de la sucursal', v_inv.existencia_minima;
    END IF;

    NEW.estado            := 'RESERVADO';
    NEW.fecha_reserva     := now();
    NEW.fecha_vencimiento := now() + make_interval(hours => v_prod.max_horas_reserva);

    UPDATE inventario
    SET cantidad_disponible = cantidad_disponible - NEW.cantidad
    WHERE sucursal_id = NEW.sucursal_id AND producto_id = NEW.producto_id;

    RETURN NEW;
END $$ LANGUAGE plpgsql;

CREATE TRIGGER trg_reserva_before_insert
BEFORE INSERT ON reserva
FOR EACH ROW EXECUTE FUNCTION fn_reserva_before_insert();

CREATE FUNCTION fn_reserva_before_update() RETURNS trigger AS $$
BEGIN
    IF OLD.estado <> 'RESERVADO' THEN
        RAISE EXCEPTION 'La reserva % está en estado %; ya no se puede modificar', OLD.id, OLD.estado;
    END IF;

    IF (to_jsonb(NEW) - 'estado') IS DISTINCT FROM (to_jsonb(OLD) - 'estado') THEN
        RAISE EXCEPTION 'Solo se permite actualizar el campo estado';
    END IF;

    IF NEW.estado NOT IN ('COMPRADO','CANCELADO') THEN
        RAISE EXCEPTION 'Desde RESERVADO solo se puede pasar a COMPRADO o CANCELADO';
    END IF;

    IF NEW.estado = 'CANCELADO' THEN
        UPDATE inventario
        SET cantidad_disponible = cantidad_disponible + OLD.cantidad
        WHERE sucursal_id = OLD.sucursal_id AND producto_id = OLD.producto_id;
    END IF;

    RETURN NEW;
END $$ LANGUAGE plpgsql;

CREATE TRIGGER trg_reserva_before_update
BEFORE UPDATE ON reserva
FOR EACH ROW EXECUTE FUNCTION fn_reserva_before_update();

CREATE TRIGGER trg_reserva_no_delete
BEFORE DELETE ON reserva
FOR EACH ROW EXECUTE FUNCTION fn_bloquear_modificacion();

CREATE TRIGGER trg_reserva_no_truncate
BEFORE TRUNCATE ON reserva
FOR EACH STATEMENT EXECUTE FUNCTION fn_bloquear_modificacion();

-- Función que usará el proceso calendarizado del entregable 2
CREATE FUNCTION fn_cancelar_reservas_vencidas() RETURNS INT AS $$
DECLARE v_n INT;
BEGIN
    UPDATE reserva SET estado = 'CANCELADO'
    WHERE estado = 'RESERVADO' AND fecha_vencimiento < now();
    GET DIAGNOSTICS v_n = ROW_COUNT;
    RETURN v_n;
END $$ LANGUAGE plpgsql;

-- ---------------------------------------------------------------------
-- VISTAS
-- ---------------------------------------------------------------------
CREATE VIEW vw_productos_inventario AS
SELECT p.id AS producto_id, p.nombre AS producto, p.categoria, p.precio,
       p.max_cantidad_reserva, p.max_horas_reserva, p.atributos,
       s.id AS sucursal_id, s.nombre AS sucursal,
       i.cantidad_disponible,
       COALESCE(r.reservada, 0) AS cantidad_reservada,
       i.existencia_minima
FROM inventario i
JOIN producto p ON p.id = i.producto_id
JOIN sucursal s ON s.id = i.sucursal_id
LEFT JOIN (
    SELECT sucursal_id, producto_id, SUM(cantidad) AS reservada
    FROM reserva WHERE estado = 'RESERVADO'
    GROUP BY sucursal_id, producto_id
) r ON r.sucursal_id = i.sucursal_id AND r.producto_id = i.producto_id;

CREATE VIEW vw_reservas_clientes AS
SELECT r.id AS reserva_id, c.id AS cliente_id, c.nombre AS cliente,
       s.nombre AS sucursal, p.nombre AS producto, r.cantidad,
       r.fecha_reserva, r.fecha_vencimiento,
       r.estado AS resultado
FROM reserva r
JOIN cliente  c ON c.id = r.cliente_id
JOIN sucursal s ON s.id = r.sucursal_id
JOIN producto p ON p.id = r.producto_id;

-- ---------------------------------------------------------------------
-- DATOS DE PRUEBA (todas las tablas)
-- ---------------------------------------------------------------------
INSERT INTO sucursal (nombre, ubicacion, activa) VALUES
 ('Pulpería Central San José', 'San José, Avenida Central', TRUE),
 ('Pulpería Heredia',          'Heredia, frente al parque', TRUE),
 ('Pulpería Cartago',          'Cartago, costado de la basílica', TRUE),
 ('Pulpería Liberia',          'Liberia, Guanacaste', TRUE);

INSERT INTO cliente (nombre, correo, telefono) VALUES
 ('María Fernández Soto', 'maria.fernandez@mail.com', '8888-1111'),
 ('José Pablo Rojas',     'jose.rojas@mail.com',      '8888-2222'),
 ('Ana Lucía Mora',       'ana.mora@mail.com',        '8888-3333'),
 ('Carlos Jiménez',       'carlos.jimenez@mail.com',  '8888-4444'),
 ('Marisol Vargas',       'marisol.vargas@mail.com',  '8888-5555');

INSERT INTO producto (nombre, categoria, precio, max_cantidad_reserva, max_horas_reserva, atributos) VALUES
 ('Zapatos de mujer',        'Calzado',         25000, 3, 48, '{"talla": 37, "color": "negro", "material": "cuero"}'),
 ('Chancletas Crocs niño',   'Calzado',         18000, 1,  6, '{"talla": 30, "color": "azul", "edad": "6-8 años"}'),
 ('Camisa casual',           'Ropa',            12000, 2, 24, '{"talla": "M", "color": "blanca", "tela": "algodón"}'),
 ('Laptop 14"',              'Electrónica',    450000, 1, 12, '{"ram_gb": 16, "cpu": "Ryzen 5", "almacenamiento_gb": 512}'),
 ('Arroz 1 kg',              'Abarrotes',        1200, 5, 72, '{"peso_kg": 1, "marca": "Tío Pelón", "vence": "2027-06-30"}'),
 ('Licuadora 600W',          'Electrodomésticos', 32000, 1, 24, '{"potencia_w": 600, "capacidad_l": 1.5, "garantia_meses": 12}');

-- Ingresos de inventario (el trigger actualiza la tabla inventario)
INSERT INTO ingreso_inventario (sucursal_id, producto_id, cantidad) VALUES
 (1,1,10),(1,2,8),(1,3,20),(1,4,5),(1,5,50),
 (2,1,6),(2,3,15),(2,4,4),(2,5,30),(2,6,7),
 (3,2,12),(3,3,10),(3,5,40),(3,6,5),
 (4,1,4),(4,4,3),(4,6,2);

-- Existencias mínimas (TODO del profe)
UPDATE inventario SET existencia_minima = 2 WHERE sucursal_id = 1 AND producto_id = 1;
UPDATE inventario SET existencia_minima = 1 WHERE sucursal_id = 4 AND producto_id = 4;
UPDATE inventario SET existencia_minima = 5 WHERE producto_id = 5;

-- Reservas (el trigger calcula vencimiento y descuenta inventario)
INSERT INTO reserva (cliente_id, sucursal_id, producto_id, cantidad, fecha_vencimiento) VALUES
 (1,1,1,2, now()),   -- id 1: queda RESERVADO
 (2,1,2,1, now()),   -- id 2: pasará a COMPRADO
 (3,2,3,2, now()),   -- id 3: queda RESERVADO
 (1,3,5,3, now()),   -- id 4: queda RESERVADO
 (4,2,4,1, now()),   -- id 5: pasará a CANCELADO
 (5,1,1,1, now());   -- id 6: pasará a COMPRADO

UPDATE reserva SET estado = 'COMPRADO'  WHERE id IN (2, 6);
UPDATE reserva SET estado = 'CANCELADO' WHERE id = 5;

-- Verificación rápida
SELECT * FROM vw_productos_inventario ORDER BY sucursal_id, producto_id;
SELECT * FROM vw_reservas_clientes ORDER BY reserva_id;

