// wwwroot/js/fal-init.js
(async () => {
    // ESM’yi dinamik import ediyoruz; Razor hiç görmüyor
    const mod = await import("https://cdn.jsdelivr.net/npm/@fal-ai/client/+esm");
    // global'e koy: window.fal
    window.fal = mod.fal;
})();
