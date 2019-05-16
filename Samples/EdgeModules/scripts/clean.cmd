::

@for /f "usebackq" %%f in (`dir /b /a:d`) do @(
    @echo checking %%f\cs
    if exist %%f\cs @(
        pushd %%f\cs
        echo cd %%fs\cs
    ) else @(
        pushd .
        echo no cs
    )
    if exist bin @(
        echo rd bin
        rd /s /q bin
    )
    if exist obj @(
        echo rd obj
        rd /s /q obj
    )
    popd
)