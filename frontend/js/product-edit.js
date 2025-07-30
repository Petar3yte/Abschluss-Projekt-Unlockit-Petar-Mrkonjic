document.addEventListener('DOMContentLoaded', async () => {
    const token = localStorage.getItem('authToken');
    if (!token) { window.location.href = 'login.html'; return; }

    const params = new URLSearchParams(window.location.search);
    const productUuid = params.get('uuid');
    if (!productUuid) { window.location.href = 'products.html'; return; }
    
    const productForm = document.getElementById('product-form');
    const imageGallery = document.getElementById('image-gallery');
    const addImageBtn = document.getElementById('add-image-btn');
    const imageUploadInput = document.getElementById('image-upload');
    
    let currentProductId = 0;

    async function loadAndPopulateForm() {
        try {
            const [prodRes, catRes, platRes, genreRes, brandsRes] = await Promise.all([
                fetch(`https://localhost:7007/api/Products/${productUuid}`, { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('https://localhost:7007/api/Categories', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('https://localhost:7007/api/Products/platforms', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('https://localhost:7007/api/Products/genres', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('https://localhost:7007/api/attributes/brands', { headers: { 'Authorization': `Bearer ${token}` } }) 
            ]);
    
            if (!prodRes.ok) throw new Error('Produktdaten konnten nicht geladen werden.');
            const product = await prodRes.json();
            currentProductId = product.productId;
    
            
            document.getElementById('name').value = product.name;
            document.getElementById('description').value = product.description;
            document.getElementById('price').value = product.price;
            document.getElementById('stock').value = product.stockQuantity;
            
            
            const categorySelect = document.getElementById('category');
            categorySelect.innerHTML = ''; 
            (await catRes.json()).forEach(c => {
                const option = document.createElement('option');
                option.value = c.categoryId;
                option.textContent = c.name;
                if (c.categoryId === product.categoryId) {
                    option.selected = true;
                }
                categorySelect.appendChild(option);
            });
    
            populateCheckboxes('platforms-container', await platRes.json(), product.platforms);
            populateCheckboxes('genres-container', await genreRes.json(), product.genres);
            
            const brandSelect = document.getElementById('brand');
            if(brandSelect && brandsRes.ok) {
                populateSelect('brand', await brandsRes.json(), product.brandId);
            }
    
            await loadImages();
            await loadRecentOrders(productUuid);
    
        } catch (error) { 
            console.error('Fehler beim Laden der Formulardaten:', error); 
            alert('Ein Fehler ist aufgetreten. Prüfe die Konsole für mehr Details.');
        }
    }

    function populateCheckboxes(containerId, items, selectedNames) {
        const container = document.getElementById(containerId);
        if (!container) return;
        container.innerHTML = '';
        const safeSelectedNames = selectedNames || [];
    
        items.forEach(item => {
            const div = document.createElement('div');
            div.className = 'form-check';
            div.innerHTML = `
                <input class="form-check-input" type="checkbox" id="${containerId}-${item.id || item.platformId || item.genreId}" 
                       value="${item.id || item.platformId || item.genreId}" 
                       ${safeSelectedNames.includes(item.name) ? 'checked' : ''}>
                <label class="form-check-label" for="${containerId}-${item.id || item.platformId || item.genreId}">${item.name}</label>
            `;
            container.appendChild(div);
        });
    }

    async function loadRecentOrders(productUuid) {
        const container = document.getElementById('recent-orders-container');
        const token = localStorage.getItem('authToken'); // Token für die Autorisierung
        
        if (!container) return; 
        
        try {
            const response = await fetch(`https://localhost:7007/api/products/${productUuid}/recent-orders`, { 
                headers: { 'Authorization': `Bearer ${token}` } 
            });
            if (!response.ok) throw new Error('Bestelldaten konnten nicht geladen werden.');
            
            const orders = await response.json();
            
            if (orders.length === 0) {
                container.innerHTML = '<p class="text-muted">Für dieses Produkt gibt es keine Bestellungen.</p>';
                return;
            }
    
            const table = document.createElement('table');
            table.className = 'table table-dark table-striped table-hover';
            table.innerHTML = `
                <thead>
                    <tr>
                        <th>Bestell-ID</th>
                        <th>Kunde</th>
                        <th>Datum</th>
                        <th class="text-end">Aktion</th>
                    </tr>
                </thead>
                <tbody>
                    ${orders.map(order => `
                        <tr>
                            <td><small>${order.orderUUID}</small></td>
                            <td>${order.customerName}</td>
                            <td>${new Date(order.orderDate).toLocaleDateString('de-DE')}</td>
                            <td class="text-end">
                                <a href="order-detail.html?uuid=${order.orderUUID}" class="btn btn-sm btn-info">Details anzeigen</a>
                            </td>
                        </tr>
                    `).join('')}
                </tbody>
            `;
            container.innerHTML = '';
            container.appendChild(table);
    
        } catch (error) { 
            console.error(error);
            container.innerHTML = `<p class="text-danger">${error.message}</p>`; 
        }
    }

    async function loadImages() {
        if (!currentProductId) return;
        const response = await fetch(`https://localhost:7007/api/Products/${currentProductId}/images`, { headers: { 'Authorization': `Bearer ${token}` } });
        const images = await response.json();
        imageGallery.innerHTML = '';
        
        if (images.length === 0) {
            imageGallery.innerHTML = '<p class="text-muted">Keine Bilder vorhanden.</p>';
        } else {
            images.forEach(img => {
                const col = document.createElement('div');
                col.className = 'col-md-4 mb-3';
                const mainBadge = img.isMainImage ? '<span class="badge bg-success position-absolute top-0 start-0 m-2">Hauptbild</span>' : '';
                const mainButton = img.isMainImage ? '<button class="btn btn-sm btn-light" disabled>Hauptbild</button>' : `<button class="btn btn-sm btn-outline-success set-main-btn" data-image-id="${img.productImageId}">Als Hauptbild</button>`;
                col.innerHTML = `<div class="card position-relative">${mainBadge}<img src="https://localhost:7007${img.imageUrl}" class="card-img-top" alt="Produktbild"><div class="card-footer d-flex justify-content-between">${mainButton}<button class="btn btn-sm btn-danger delete-image-btn" data-image-id="${img.productImageId}">Löschen</button></div></div>`;
                imageGallery.appendChild(col);
            });
        }
    }

    async function loadRecentOrders(productUuid) {
        const container = document.getElementById('recent-orders-container');
        if (!container) return; 
        try {
            const response = await fetch(`https://localhost:7007/api/products/${productUuid}/recent-orders`, { headers: { 'Authorization': `Bearer ${token}` } });
            if (!response.ok) throw new Error('Bestelldaten konnten nicht geladen werden.');
            const orders = await response.json();
            if (orders.length === 0) {
                container.innerHTML = '<p class="text-muted">Für dieses Produkt gibt es keine Bestellungen.</p>';
            } else {
                container.innerHTML = `
                    <table class="table table-dark table-striped">
                        <thead><tr><th>Bestell-ID</th><th>Kunde</th><th>Datum</th><th class="text-end">Aktion</th></tr></thead>
                        <tbody>${orders.map(order => `<tr><td><small>${order.orderUUID}</small></td><td>${order.customerName}</td><td>${new Date(order.orderDate).toLocaleDateString('de-DE')}</td><td class="text-end"><a href="order-detail.html?uuid=${order.orderUUID}" class="btn btn-sm btn-info">Details</a></td></tr>`).join('')}</tbody>
                    </table>`;
            }
        } catch (error) { container.innerHTML = `<p class="text-danger">${error.message}</p>`; }
    }    

    if (productForm) productForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        const productData = {
            name: document.getElementById('name').value,
            description: document.getElementById('description').value,
            price: parseFloat(document.getElementById('price').value),
            stockQuantity: parseInt(document.getElementById('stock').value),
            categoryId: parseInt(document.getElementById('category').value),
            isVisible: true,
            platformIds: Array.from(document.querySelectorAll('#platforms-container input:checked')).map(cb => parseInt(cb.value)),
            genreIds: Array.from(document.querySelectorAll('#genres-container input:checked')).map(cb => parseInt(cb.value))
        };
        try {
            const response = await fetch(`https://localhost:7007/api/Products/${productUuid}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify(productData)
            });
            if (response.ok) { window.location.href = 'products.html'; } 
            else { throw new Error('Update fehlgeschlagen'); }
        } catch(error) { console.error('Fehler beim Update:', error); }
    });

    if (addImageBtn) addImageBtn.addEventListener('click', async () => {
        if (imageUploadInput.files.length === 0 || !currentProductId) return;
        const formData = new FormData();
        formData.append('file', imageUploadInput.files[0]);
        try {
            await fetch(`https://localhost:7007/api/Products/${currentProductId}/upload-image`, {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}` },
                body: formData
            });
            imageUploadInput.value = '';
            await loadImages();
        } catch (error) { console.error('Fehler beim Bild-Upload:', error); }
    });

    if (imageGallery) imageGallery.addEventListener('click', async (e) => {
        if (e.target.classList.contains('delete-image-btn')) {
            if (!confirm('Bild wirklich löschen?')) return;
            await fetch(`https://localhost:7007/api/Products/images/${e.target.dataset.imageId}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
            await loadImages();
        }
        if (e.target.classList.contains('set-main-btn')) {
            await fetch(`https://localhost:7007/api/Products/images/${e.target.dataset.imageId}/set-main`, { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } });
            await loadImages();
        }
    });

    loadAndPopulateForm();
});