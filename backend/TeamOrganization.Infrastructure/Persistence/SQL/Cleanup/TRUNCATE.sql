DO $$
BEGIN
    IF to_regclass('public.users') IS NOT NULL THEN
        TRUNCATE TABLE public.users CASCADE;
    END IF;
END
$$;