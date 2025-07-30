document.addEventListener('DOMContentLoaded', async () => {
    const token = localStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'admin-login.html';
        return;
    }

    const itemsContainer = document.getElementById('delivery-items-container');
    const addItemBtn = document.getElementById('add-item-btn');
    const deliveryForm = document.getElementById('delivery-form');
    const feedbackDiv = document.getElementById('feedback-message');
    
    let allProducts = []; 

    try {
        const response = await fetch('https://localhost:7007/api/Products', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (!response.ok) throw new Error('Produkte konnten nicht geladen werden.');
        allProducts = await response.json();
        addNewItemRow();
    } catch (error) {
        showFeedback(`Fehler beim Laden der Produkte: ${error.message}`, false);
    }

    function addNewItemRow() {
        const template = document.getElementById('delivery-item-template');
        const newItemRow = template.cloneNode(true);
        newItemRow.removeAttribute('id');
        newItemRow.style.display = 'flex';

        const productSelect = newItemRow.querySelector('.product-select');
        allProducts.forEach(product => {
            const option = document.createElement('option');
            option.value = product.productUUID;
            option.textContent = product.name;
            productSelect.appendChild(option);
        });
        
        newItemRow.querySelector('.remove-item-btn').addEventListener('click', () => {
            newItemRow.remove();
        });

        itemsContainer.appendChild(newItemRow);
    }

    addItemBtn.addEventListener('click', addNewItemRow);

    deliveryForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        
        const deliveryItems = [];
        const itemRows = itemsContainer.querySelectorAll('.row');

        itemRows.forEach(row => {
            const productUuid = row.querySelector('.product-select').value;
            const quantity = parseInt(row.querySelector('.quantity-input').value, 10);
            const costPerItem = parseFloat(row.querySelector('.cost-input').value);

            if (productUuid && quantity > 0 && costPerItem >= 0) {
                deliveryItems.push({ productUuid, quantity, costPerItem });
            }
        });

        if (deliveryItems.length === 0) {
            showFeedback('Bitte fügen Sie mindestens eine gültige Lieferposition hinzu.', false);
            return;
        }

        try {
            const response = await fetch('https://localhost:7007/api/Products/record-delivery', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(deliveryItems)
            });

            const result = await response.json();
            if (!response.ok) throw new Error(result.message || 'Unbekannter Fehler');

            showFeedback(result.message, true);
            itemsContainer.innerHTML = '';
            addNewItemRow();

        } catch (error) {
            showFeedback(`Fehler: ${error.message}`, false);
        }
    });

    function showFeedback(message, isSuccess) {
        feedbackDiv.textContent = message;
        feedbackDiv.className = isSuccess ? 'alert alert-success mt-3' : 'alert alert-danger mt-3';
    }
});