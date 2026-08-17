-- -----------------------------------------------------------------------------
-- Normalización de los motivos de las alternativas sugeridas
-- -----------------------------------------------------------------------------
-- El motivo es el texto que el vendedor lee en el mostrador. Antes se escribía
-- libre, así que la misma idea quedó redactada de varias formas ("más económico"
-- y "Más económico" conviven hoy). La ficha ahora ofrece los motivos frecuentes
-- como botones, lo que evita que siga pasando, pero no arregla lo ya cargado.
--
-- Solo se tocan los que coinciden con un motivo conocido ignorando mayúsculas y
-- espacios. El texto libre se respeta: si alguien escribió algo propio, tendrá
-- sus razones y no es tarea de una migración decidir que estaba mal.
--
-- Idempotente: correrla de nuevo no cambia nada.

DO $$
DECLARE
    v_motivos text[] := ARRAY[
        'Más económico',
        'Misma composición',
        'Cuando no hay stock',
        'El cliente lo prefiere'
    ];
    v_canonico text;
    v_total    integer := 0;
    v_filas    integer;
BEGIN
    FOREACH v_canonico IN ARRAY v_motivos
    LOOP
        UPDATE public.product_alternatives
           SET reason = v_canonico,
               modified = now()
         WHERE reason IS NOT NULL
           AND lower(trim(reason)) = lower(v_canonico)
           AND reason <> v_canonico;

        GET DIAGNOSTICS v_filas = ROW_COUNT;
        IF v_filas > 0 THEN
            RAISE NOTICE '% : % fila(s) normalizada(s).', v_canonico, v_filas;
        END IF;
        v_total := v_total + v_filas;
    END LOOP;

    IF v_total = 0 THEN
        RAISE NOTICE 'No había motivos que normalizar.';
    ELSE
        RAISE NOTICE 'Total normalizado: % fila(s).', v_total;
    END IF;
END $$;
