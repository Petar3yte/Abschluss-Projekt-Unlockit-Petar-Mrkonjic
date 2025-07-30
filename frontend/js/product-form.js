document.addEventListener('DOMContentLoaded', async () => {

    const API_BASE_URL = 'https://localhost:7007'; 
    const token = localStorage.getItem('authToken');
    if (!token) { window.location.href = 'login.html'; return; }

    const productForm = document.getElementById('product-form');
    const pageTitle = document.querySelector('.container h1');
    const errorMessageDiv = document.getElementById('error-message');
    const categorySelect = document.getElementById('category');
    const platformsContainer = document.getElementById('platforms-container');
    const genresContainer = document.getElementById('genres-container');
    const submitButton = productForm.querySelector('button[type="submit"]');

    const imageManagementSection = document.getElementById('image-management-section');
    const imageGallery = document.getElementById('image-gallery');
    const addImageBtn = document.getElementById('add-image-btn');
    const imageUploadInput = document.getElementById('image-upload');

    let currentProductId = null;

    async function loadOptions() {
        try {
            const [catRes, platRes, genreRes] = await Promise.all([
                fetch(`${API_BASE_URL}/api/Products/categories`, { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch(`${API_BASE_URL}/api/Products/platforms`, { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch(`${API_BASE_URL}/api/Products/genres`, { headers: { 'Authorization': `Bearer ${token}` } })
            ]);
            
            const categories = await catRes.json();
            const platforms = await platRes.json();
            const genres = await genreRes.json();

            categorySelect.innerHTML += categories.map(c => `<option value="${c.categoryId}">${c.name}</option>`).join('');

            platforms.forEach(p => {
                platformsContainer.innerHTML += `
                    <div class="form-check">
                        <input class="form-check-input" type="checkbox" value="${p.platformId}" id="plat-${p.platformId}">
                        <label class="form-check-label" for="plat-${p.platformId}">${p.name}</label>
                    </div>`;
            });

            genres.forEach(g => {
                genresContainer.innerHTML += `
                    <div class="form-check">
                        <input class="form-check-input" type="checkbox" value="${g.genreId}" id="genre-${g.genreId}">
                        <label class="form-check-label" for="genre-${g.genreId}">${g.name}</label>
                    </div>`;
            });

        } catch (error) { console.error('Fehler beim Laden der Optionen:', error); }
    }

    productForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        errorMessageDiv.classList.add('d-none');

        const platformIds = Array.from(platformsContainer.querySelectorAll('input:checked')).map(cb => parseInt(cb.value));
        const genreIds = Array.from(genresContainer.querySelectorAll('input:checked')).map(cb => parseInt(cb.value));

        const productData = {
            name: document.getElementById('name').value,
            description: document.getElementById('description').value,
            price: parseFloat(document.getElementById('price').value),
            stockQuantity: parseInt(document.getElementById('stock').value, 10),
            categoryId: parseInt(document.getElementById('category').value, 10),
            platformIds: platformIds,
            genreIds: genreIds
        };

        try {
            const response = await fetch(`${API_BASE_URL}/api/Products`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify(productData)
            });

            if (response.ok) { 
                const newProduct = await response.json();
                currentProductId = newProduct.productId; 

                pageTitle.textContent = `Produkt bearbeiten: "${newProduct.name}"`;
                submitButton.disabled = true;

                productForm.querySelectorAll('input, textarea, select').forEach(el => el.disabled = true);
                
                imageManagementSection.classList.remove('d-none');
                imageManagementSection.scrollIntoView({ behavior: 'smooth' });
                
                document.getElementById('create-actions').classList.add('d-none');
                document.getElementById('after-create-actions').classList.remove('d-none');

            } else { 
                throw new Error('Produkt konnte nicht erstellt werden.'); 
            }
        } catch (error) {
            errorMessageDiv.textContent = error.message;
            errorMessageDiv.classList.remove('d-none');
        }
    });
    
    
    async function loadImages() {
        if (!currentProductId) return;
        
        try {
            const response = await fetch(`${API_BASE_URL}/api/Products/${currentProductId}/images`, { headers: { 'Authorization': `Bearer ${token}` } });
            const images = await response.json();
            imageGallery.innerHTML = '';
            images.forEach(img => {
                const col = document.createElement('div');
                col.className = 'col-md-4 mb-3';
                col.innerHTML = `
                    <div class="card">
                        <img src="${API_BASE_URL}${img.imageUrl}" class="card-img-top" style="height: 150px; object-fit: cover;" alt="Produktbild">
                        <div class="card-footer text-center">
                            <button class="btn btn-sm btn-danger delete-image-btn" data-image-id="${img.productImageId}">Löschen</button>
                        </div>
                    </div>`;
                imageGallery.appendChild(col);
            });
        } catch (error) { console.error('Fehler beim Laden der Bilder:', error); }
    }

    addImageBtn.addEventListener('click', async () => {
        if (imageUploadInput.files.length === 0) return alert('Bitte wählen Sie zuerst eine Bild-Datei aus.');
        if (!currentProductId) return;
        
        const formData = new FormData();
        formData.append('file', imageUploadInput.files[0]);
        
        try {
            const response = await fetch(`${API_BASE_URL}/api/Products/${currentProductId}/upload-image`, {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}` },
                body: formData
            });
            if (response.ok) {
                imageUploadInput.value = '';
                await loadImages();
            } else { alert('Fehler beim Hochladen des Bildes.'); }
        } catch (error) { console.error('Fehler beim Bild-Upload:', error); }
    });

    imageGallery.addEventListener('click', async (e) => {
        if (e.target.classList.contains('delete-image-btn')) {
            if (!confirm('Möchtest du dieses Bild wirklich löschen?')) return;
            const imageId = e.target.dataset.imageId;
            await fetch(`${API_BASE_URL}/api/Products/images/${imageId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            await loadImages();
        }
    });

    loadOptions(); 
});