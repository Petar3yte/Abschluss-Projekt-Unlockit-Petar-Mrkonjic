document.addEventListener('DOMContentLoaded', function () {
    const token = localStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'shop-login.html';
        return;
    }
    
    const userDataForm = document.getElementById('user-data-form');
    const feedbackDiv = document.getElementById('user-data-feedback');    
    const profilePicImg = document.getElementById('current-profile-pic');
    const profilePicInput = document.getElementById('profilePicInput');
    const uploadPicBtn = document.getElementById('upload-pic-btn');
    const addressListContainer = document.getElementById('address-list');
    const addAddressModalElement = document.getElementById('addAddressModal');
    const addAddressForm = document.getElementById('addAddressForm');
    const addAddressError = document.getElementById('addAddressError');
    const modalTitle = document.getElementById('addAddressModalLabel');
    const modalInstance = new bootstrap.Modal(addAddressModalElement);
    let editingAddressUuid = null;

    async function loadUserData() {
        try {
            const response = await fetch('https://localhost:7007/api/Users/me', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) {
                 throw new Error('Benutzerdaten konnten nicht geladen werden.');
            }
            const user = await response.json();
            populateUserDataForm(user);
        } catch (error) {
            feedbackDiv.innerHTML = `<div class="alert alert-danger">${error.message}</div>`;
        }
    }

    function populateUserDataForm(user) {
        document.getElementById('firstName').value = user.firstName || '';
        document.getElementById('lastName').value = user.lastName || '';
        document.getElementById('username').value = user.userName || '';
        document.getElementById('email').value = user.email || '';
        if (user.birthdate) {
            document.getElementById('birthdate').value = user.birthdate.split('T')[0];
        } else {
             document.getElementById('birthdate').value = '';
        }
        document.getElementById('password').value = '';
       
        if (user.profilePictureUrl) {
            profilePicImg.src = `https://localhost:7007${user.profilePictureUrl}`;
        } else {
            profilePicImg.src = 'https://placehold.co/150/1a1a2e/ffffff?text=Profilbild';
        }
    }
    
    uploadPicBtn.addEventListener('click', async () => {
        const file = profilePicInput.files[0];
        if (!file) {
            feedbackDiv.innerHTML = `<div class="alert alert-warning">Bitte wähle zuerst eine Bilddatei aus.</div>`;
            return;
        }

        const formData = new FormData();
        formData.append('file', file);

        try {
            feedbackDiv.innerHTML = `<div class="alert alert-info">Bild wird hochgeladen...</div>`;
            const response = await fetch('https://localhost:7007/api/Users/me/upload-profile-picture', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}` },
                body: formData
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Fehler beim Hochladen.');
            }

            const result = await response.json();

            const currentUser = JSON.parse(localStorage.getItem('currentUser'));
            currentUser.profilePictureUrl = result.profilePictureUrl;
            localStorage.setItem('currentUser', JSON.stringify(currentUser));
            
            feedbackDiv.innerHTML = `<div class="alert alert-success">Profilbild erfolgreich aktualisiert! Die Seite wird neu geladen.</div>`;
            setTimeout(() => window.location.reload(), 2000);

        } catch (error) {
            feedbackDiv.innerHTML = `<div class="alert alert-danger">${error.message}</div>`;
        }
    });

    userDataForm.addEventListener('submit', async function(event) {
        event.preventDefault();
        feedbackDiv.innerHTML = '';
        
        const passwordValue = document.getElementById('password').value;

        const updatedData = {
            firstName: document.getElementById('firstName').value,
            lastName: document.getElementById('lastName').value,
            userName: document.getElementById('username').value,
            email: document.getElementById('email').value,
            birthdate: document.getElementById('birthdate').value || null,
            password: passwordValue ? passwordValue : null
        };
        
        try {
            const response = await fetch('https://localhost:7007/api/Users/me', {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(updatedData)
            });

            const result = await response.json();

            if (!response.ok) {
                const errorData = result.errors ? JSON.stringify(result.errors) : (result.message || 'Speichern fehlgeschlagen.');
                throw new Error(errorData);
            }

            localStorage.setItem('currentUser', JSON.stringify(result.user));
            feedbackDiv.innerHTML = `<div class="alert alert-success">Daten erfolgreich aktualisiert! Die Seite wird neu geladen...</div>`;
            
            setTimeout(() => {
                window.location.reload();
            }, 1500);

        } catch (error) {
             feedbackDiv.innerHTML = `<div class="alert alert-danger">${error.message}</div>`;
        }
    });
    
    function loadUserAddresses() {
        addressListContainer.innerHTML = '<h6>Lade Adressen...</h6>';

        fetch('https://localhost:7007/api/Users/my/addresses', {
            method: 'GET',
            headers: { 'Authorization': `Bearer ${token}` }
        })
        .then(response => {
            if (response.status === 401) {
                localStorage.clear();
                window.location.href = 'shop-login.html';
                return Promise.reject(new Error('Session abgelaufen.'));
            }
            if (!response.ok) throw new Error('Failed to fetch addresses.');
            return response.json();
        })
        .then(addresses => {
            addressListContainer.innerHTML = '';
            if (addresses && addresses.length > 0) {
                addresses.forEach(address => {
                    const addressCard = document.createElement('div');
                    addressCard.className = 'card product-card mb-3';
                    const addressJson = JSON.stringify(address);

                    addressCard.innerHTML = `
                        <div class="card-body">
                            <div class="d-flex justify-content-between align-items-start">
                                <div>
                                    <h5 class="card-title mb-1">${address.name}</h5>
                                    <p class="card-text mb-0">${address.addressLine1}</p>
                                    <p class="card-text mb-0">${address.postalCode} ${address.city}</p>
                                    <p class="card-text">${address.country}</p>
                                </div>
                                <div>
                                    <button class="btn btn-sm btn-secondary-neon btn-edit-address" data-address='${addressJson}'>
                                        <i class="bi bi-pencil-fill"></i>
                                    </button>
                                    <button class="btn btn-sm btn-outline-danger btn-delete-address" data-address-uuid="${address.addressUUID}">
                                        <i class="bi bi-trash-fill"></i>
                                    </button>
                                </div>
                            </div>
                        </div>
                    `;
                    addressListContainer.appendChild(addressCard);
                });
            } else {
                addressListContainer.innerHTML = '<p>Sie haben noch keine Adressen hinterlegt.</p>';
            }
            const newAddressBtn = document.createElement('button');
            newAddressBtn.id = 'open-add-address-modal-btn';
            newAddressBtn.className = 'btn btn-primary-neon mt-3';
            newAddressBtn.textContent = 'Neue Adresse hinzufügen';
            addressListContainer.appendChild(newAddressBtn);
        })
        .catch(error => {
            console.error('Fehler beim Laden der Adressen:', error);
            addressListContainer.innerHTML = `<p class="text-danger">Ein Fehler ist aufgetreten: ${error.message}</p>`;
        });
    }

    addressListContainer.addEventListener('click', function(event) {
        const targetButton = event.target.closest('button');
        if (!targetButton) return;

        if (targetButton.classList.contains('btn-edit-address')) {
            const address = JSON.parse(targetButton.dataset.address);
            editingAddressUuid = address.addressUUID;
            modalTitle.textContent = 'Adresse bearbeiten';
            document.getElementById('addressName').value = address.name;
            document.getElementById('addressLine1').value = address.addressLine1;
            document.getElementById('city').value = address.city;
            document.getElementById('postalCode').value = address.postalCode;
            document.getElementById('country').value = address.country;
            modalInstance.show();
        }

        if (targetButton.classList.contains('btn-delete-address')) {
            const addressUuidToDelete = targetButton.dataset.addressUuid;
            if (confirm("Möchtest du diese Adresse wirklich endgültig löschen?")) {
                fetch(`https://localhost:7007/api/Addresses/${addressUuidToDelete}`, {
                    method: 'DELETE',
                    headers: { 'Authorization': `Bearer ${token}` }
                })
                .then(response => {
                    if (!response.ok) throw new Error('Löschen fehlgeschlagen.');
                    loadUserAddresses();
                })
                .catch(error => alert(error.message));
            }
        }

        if (targetButton.id === 'open-add-address-modal-btn') {
            modalInstance.show();
        }
    });
    
    addAddressModalElement.addEventListener('hidden.bs.modal', function () {
        editingAddressUuid = null;
        modalTitle.textContent = 'Neue Adresse hinzufügen';
        addAddressForm.reset();
        addAddressError.style.display = 'none';
    });

    addAddressForm.addEventListener('submit', function(event) {
        event.preventDefault();
        addAddressError.style.display = 'none';

        const addressData = {
            name: document.getElementById('addressName').value,
            addressLine1: document.getElementById('addressLine1').value,
            city: document.getElementById('city').value,
            postalCode: document.getElementById('postalCode').value,
            country: document.getElementById('country').value,
        };

        let url, method;

        if (editingAddressUuid) {
            url = `https://localhost:7007/api/Addresses/${editingAddressUuid}`;
            method = 'PUT';
        } else {
            url = 'https://localhost:7007/api/Addresses';
            method = 'POST';
        }

        fetch(url, {
            method: method,
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(addressData)
        })
        .then(response => {
            if (!response.ok) {
                return response.text().then(text => { throw new Error(text || 'Speichern fehlgeschlagen.') });
            }
            return method === 'PUT' ? null : response.json();
        })
        .then(() => {
            modalInstance.hide();
            loadUserAddresses();
        })
        .catch(error => {
            addAddressError.textContent = error.message;
            addAddressError.style.display = 'block';
        });
    });

    loadUserData();
    loadUserAddresses();
});