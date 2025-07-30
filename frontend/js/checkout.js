document.addEventListener('DOMContentLoaded', () => {
    
    initializeCheckout();

    const placeOrderBtn = document.getElementById('place-order-btn');
    if (placeOrderBtn) {
        placeOrderBtn.addEventListener('click', placeOrder);
    }

    const placeOrderAndContinueBtn = document.getElementById('place-order-and-continue-btn');
    if (placeOrderAndContinueBtn) {
        placeOrderAndContinueBtn.addEventListener('click', placeOrderAndContinue);
    }
});


async function initializeCheckout() {
    const token = localStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'shop-login.html';
        return;
    }

    const cart = JSON.parse(localStorage.getItem('unlockitCart')) || [];

    await loadAddresses(token);
    await loadCartSummary(cart);
   
    await loadPaymentMethods();
}

async function loadAddresses(token) {
    try {
        const response = await fetch('https://localhost:7007/api/Users/my/addresses', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (!response.ok) throw new Error('Adressen konnten nicht geladen werden.');
        
        const addresses = await response.json();
        const addressContainer = document.getElementById('address-selection-container');
        if (addresses.length > 0) {
            addressContainer.innerHTML = addresses.map((address, index) => `
                <div class="form-check">
                    <input id="address-${index}" name="shippingAddress" type="radio" class="form-check-input" value="${address.addressUUID}" ${index === 0 ? 'checked' : ''}>
                    <label class="form-check-label" for="address-${index}">
                        <strong>${address.name}</strong><br>
                        ${address.addressLine1}<br>
                        ${address.postalCode} ${address.city}, ${address.country}
                    </label>
                </div>
            `).join('');
        } else {
            addressContainer.innerHTML = '<p>Bitte legen Sie zuerst unter "Mein Konto" eine Adresse an.</p>';
            document.getElementById('place-order-btn').disabled = true;
        }
    } catch (error) {
        console.error('Fehler beim Laden der Adressen:', error);
        document.getElementById('address-selection-container').innerHTML = '<p class="text-danger">Fehler beim Laden der Adressen.</p>';
    }
}

async function loadCartSummary(cart) {
    const summaryList = document.getElementById('summary-item-list');
    const summaryCounter = document.getElementById('summary-cart-counter');
    const summaryTotal = document.getElementById('summary-total');

    if (cart.length === 0) {
        summaryList.innerHTML = '<li class="list-group-item list-group-item-dark">Warenkorb ist leer</li>';
        return;
    }

    try {
        const productUuids = cart.map(item => item.uuid);
        const response = await fetch('https://localhost:7007/api/Products/details', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(productUuids)
        });
        if (!response.ok) throw new Error('Produktdetails konnten nicht geladen werden.');
        
        const productDetails = await response.json();
        let total = 0;
        
        const itemsFragment = document.createDocumentFragment();

        cart.forEach(item => {
            const detail = productDetails.find(d => d.productUUID === item.uuid);
            if (detail) {
                total += detail.price * item.quantity;
                const summaryItem = document.createElement('li');
                summaryItem.className = 'list-group-item d-flex justify-content-between lh-sm';
                summaryItem.innerHTML = `
                    <div>
                        <h6 class="my-0">${detail.name}</h6>
                        <small class="text-body-secondary">Menge: ${item.quantity}</small>
                    </div>
                    <span class="text-body-secondary">${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(detail.price * item.quantity)}</span>
                `;
                itemsFragment.appendChild(summaryItem);
            }
        });
        while (summaryList.children.length > 1) {
            summaryList.removeChild(summaryList.firstChild);
        }
        summaryList.insertBefore(itemsFragment, summaryList.lastElementChild);
        
        summaryCounter.textContent = cart.reduce((sum, item) => sum + item.quantity, 0);
        summaryTotal.textContent = new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(total);

    } catch (error) {
        console.error('Fehler beim Laden der Warenkorb-Zusammenfassung:', error);
        summaryList.innerHTML = `<li class="list-group-item text-danger">Fehler: ${error.message}</li>`;
    }
}


async function loadPaymentMethods() {
    const paymentMethodsContainer = document.getElementById('payment-methods-container');
    try {
        const response = await fetch('https://localhost:7007/api/paymentmethods/active');
        if (!response.ok) {
            throw new Error('Zahlungsmethoden konnten nicht geladen werden.');
        }
        const methods = await response.json();

        paymentMethodsContainer.innerHTML = ''; 
        if (methods.length === 0) {
             paymentMethodsContainer.innerHTML = '<p class="text-danger">Keine Zahlungsmethoden verfügbar.</p>';
             return;
        }

        methods.forEach((method, index) => {
            const div = document.createElement('div');
            div.className = 'form-check';
            div.innerHTML = `
                <input id="payment-${method.paymentMethodId}" name="paymentMethod" type="radio" 
                       class="form-check-input" value="${method.name}" ${index === 0 ? 'checked' : ''} required>
                <label class="form-check-label" for="payment-${method.paymentMethodId}">${method.name}</label>
            `;
            paymentMethodsContainer.appendChild(div);
        });

    } catch (error) {
        console.error(error);
        paymentMethodsContainer.innerHTML = `<p class="text-danger">${error.message}</p>`;
    }
}

async function placeOrder() {
    const orderPayload = getOrderPayload();
    if (!orderPayload) return;

    try {
        const token = localStorage.getItem('authToken');
        const response = await fetch('https://localhost:7007/api/Orders', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(orderPayload)
        });

        if (response.ok) {
            alert('Vielen Dank, Ihre Bestellung wurde aufgegeben!');
            localStorage.removeItem('unlockitCart');
            window.location.href = 'my-orders.html'; // Leitet zur Bestellübersicht
        } else {
            const errorResult = await response.json();
            throw new Error(errorResult.message || 'Die Bestellung konnte nicht verarbeitet werden.');
        }
    } catch (error) {
        console.error('Fehler beim Absenden der Bestellung:', error);
        alert(`Fehler: ${error.message}`);
    }
}

async function placeOrderAndContinue() {
    const orderPayload = getOrderPayload();
    if (!orderPayload) return;

    try {
        const token = localStorage.getItem('authToken');
        const response = await fetch('https://localhost:7007/api/Orders', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(orderPayload)
        });

        if (response.ok) {
            alert('Vielen Dank, Ihre Bestellung wurde aufgegeben!');
            localStorage.removeItem('unlockitCart');
            window.location.href = 'index.html'; // Leitet zur Startseite
        } else {
            const errorResult = await response.json();
            throw new Error(errorResult.message || 'Die Bestellung konnte nicht verarbeitet werden.');
        }
    } catch (error) {
        console.error('Fehler beim Absenden der Bestellung:', error);
        alert(`Fehler: ${error.message}`);
    }
}

function getOrderPayload() {
    const cart = JSON.parse(localStorage.getItem('unlockitCart')) || [];
    const selectedAddress = document.querySelector('input[name="shippingAddress"]:checked');
    const selectedPayment = document.querySelector('input[name="paymentMethod"]:checked');

    if (!selectedAddress) {
        alert('Bitte wählen Sie eine Lieferadresse.');
        return null;
    }
    if (!selectedPayment) {
        alert('Bitte wählen Sie eine Zahlungsmethode.');
        return null;
    }
    
    return {
        shippingAddressUUID: selectedAddress.value,
        paymentMethodName: selectedPayment.value,
        items: cart.map(item => ({ productUuid: item.uuid, quantity: item.quantity }))
    };
}