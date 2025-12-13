// wwwroot/js/page-index.js
console.log("[nano-banana] page loaded");

const API_BASE = "https://localhost:7122";

function getJWT() {
    const val = (document.querySelector("#jwt")?.value || "").trim();
    return val || null;
}

// Tek dosya yükle
async function uploadSingleFile(file) {
    const formData = new FormData();
    formData.append("file", file);

    const res = await fetch(`${API_BASE}/api/fal/storage/upload`, {
        method: "POST",
        headers: {
            ...(getJWT() ? { Authorization: `Bearer ${getJWT()}` } : {})
        },
        body: formData
    });

    if (!res.ok) {
        const err = await res.text();
        throw new Error(`Upload failed: ${err}`);
    }

    const json = await res.json();
    return json.file_url;
}

// Birden fazla dosya yükle
async function uploadAllFiles(files) {
    const urls = [];
    for (const file of files) {
        const url = await uploadSingleFile(file);
        urls.push(url);
    }
    return urls;
}

// Nano-Banana edit API
async function runNanoBananaEdit(imageUrls, prompt, numImages, outputFormat) {
    const formData = new FormData();
    formData.append("Prompt", prompt);

    // ÖNEMLİ: Boş değerleri GÖNDERME
    if (numImages && numImages > 0) {
        formData.append("NumImages", numImages);
    }
    if (outputFormat && outputFormat.trim()) {
        formData.append("OutputFormat", outputFormat);
    }

    // URL'leri ekle
    for (const url of imageUrls) {
        formData.append("ImageUrls", url);
    }

    console.log("FormData içeriği:");
    for (let [key, value] of formData.entries()) {
        console.log(`  ${key}: ${value}`);
    }

    const res = await fetch(`${API_BASE}/api/fal/nano-banana/image-edit-form`, {
        method: "POST",
        headers: {
            ...(getJWT() ? { Authorization: `Bearer ${getJWT()}` } : {})
        },
        body: formData
    });

    if (!res.ok) {
        const contentType = res.headers.get("content-type");
        let errorText;

        if (contentType && contentType.includes("application/json")) {
            const errorJson = await res.json();
            errorText = JSON.stringify(errorJson);
        } else {
            errorText = await res.text();
        }

        console.error("API Error:", errorText);
        throw new Error(`API failed: ${errorText}`);
    }

    return await res.blob();
}

window.addEventListener("DOMContentLoaded", () => {
    const $ = (s) => document.querySelector(s);

    const fileInput = $("#fileInput");
    const runBtn = $("#runBtn");
    const statusEl = $("#status");
    const urlsEl = $("#urls");
    const resultsEl = $("#results");
    const promptEl = $("#prompt");
    const numImagesEl = $("#numImages");
    const outputFormatEl = $("#outputFormat");

    runBtn?.addEventListener("click", async () => {
        try {
            resultsEl.innerHTML = "";
            urlsEl.textContent = "";
            statusEl.textContent = "";

            const files = Array.from(fileInput?.files || []);
            if (!files.length) {
                alert("En az bir görsel seçin.");
                return;
            }

            const jwt = getJWT();
            if (!jwt) {
                alert("JWT token girin");
                return;
            }

            // 1) Upload
            statusEl.textContent = "Dosyalar yükleniyor...";
            const imageUrls = await uploadAllFiles(files);

            urlsEl.textContent = "Yüklenen URL'ler:\n" +
                JSON.stringify(imageUrls, null, 2);

            console.log("Yüklenen URL'ler:", imageUrls);

            // 2) Model çalıştır
            statusEl.textContent = "Model çalışıyor...";
            const prompt = (promptEl?.value || "").trim();
            if (!prompt) {
                alert("Prompt gerekli");
                return;
            }

            // Boş değerleri parse etme
            const numImagesVal = (numImagesEl?.value || "").trim();
            const numImages = numImagesVal ? parseInt(numImagesVal, 10) : null;

            const outputFormat = (outputFormatEl?.value || "").trim();

            const blob = await runNanoBananaEdit(
                imageUrls,
                prompt,
                numImages,
                outputFormat || null
            );

            // 3) Sonuç
            const objUrl = URL.createObjectURL(blob);
            const img = document.createElement("img");
            img.style.maxWidth = "400px";
            img.style.borderRadius = "8px";
            img.src = objUrl;
            resultsEl.appendChild(img);

            statusEl.textContent = "✓ Tamamlandı";
            statusEl.style.color = "green";
        } catch (e) {
            console.error(e);
            statusEl.textContent = `✗ Hata: ${e.message}`;
            statusEl.style.color = "red";
            alert(`Hata: ${e.message}`);
        }
    });
});