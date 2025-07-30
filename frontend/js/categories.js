document.addEventListener('DOMContentLoaded', () => {    
    const token = localStorage.getItem('authToken');
    if (!token) { window.location.href = 'login.html'; return; }

    const logoutButton = document.getElementById('logout-button');
    if (logoutButton) {
        logoutButton.addEventListener('click', () => {
            localStorage.removeItem('authToken');
            localStorage.removeItem('currentUser');
            window.location.href = 'login.html';
        });
    }
    
    const categoryTableBody = document.getElementById('category-table-body');
    const categoryForm = document.getElementById('category-form');
    const categoryModal = new bootstrap.Modal(document.getElementById('categoryModal'));
    const categoryModalLabel = document.getElementById('categoryModalLabel');
    const categoryIdInput = document.getElementById('edit-category-id'); // Das versteckte Feld für die ID
    const nameInput = document.getElementById('name');
    const descriptionInput = document.getElementById('description');

    async function fetchCategories() {
        try {
            const response = await fetch('https://localhost:7007/api/Categories', { headers: { 'Authorization': `Bearer ${token}` } });
            const categories = await response.json();
            categoryTableBody.innerHTML = ''; // Leert die Tabelle vor dem Neuaufbau
            categories.forEach(cat => {
                const row = document.createElement('tr');
                row.innerHTML = `
                    <td>${cat.name}</td>
                    <td>${cat.description || ''}</td>
                    <td class="text-end">
                        <button class="btn btn-sm btn-info btn-edit" 
                            data-bs-toggle="modal" data-bs-target="#categoryModal"
                            data-id="${cat.categoryId}" data-name="${cat.name}" data-description="${cat.description || ''}">
                            Bearbeiten
                        </button>
                        <button class="btn btn-sm btn-danger btn-delete" data-id="${cat.categoryId}">
                            Löschen
                        </button>
                    </td>
                `;
                categoryTableBody.appendChild(row);
            });
        } catch (error) { console.error('Fehler beim Laden der Kategorien:', error); }
    }

    async function handleFormSubmit(event) {
        event.preventDefault(); 
        const id = categoryIdInput.value;
        const categoryData = {
            categoryId: id ? parseInt(id) : 0,
            name: nameInput.value,
            description: descriptionInput.value
        };

        const isEdit = id ? true : false;
        const url = isEdit ? `https://localhost:7007/api/Categories/${id}` : 'https://localhost:7007/api/Categories';
        const method = isEdit ? 'PUT' : 'POST';

        try {
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
                body: JSON.stringify(categoryData)
            });

            if (response.ok) {
                categoryModal.hide(); 
                await fetchCategories(); 
            } else { alert('Aktion fehlgeschlagen.'); }
        } catch (error) { console.error('Fehler:', error); }
    }

    function handleTableClick(event) {
        const target = event.target;
        if (target.classList.contains('btn-delete')) {
            if (confirm('Diese Kategorie wirklich löschen? Dadurch wird die Kategorie bei betroffenen Produkten entfernt.')) {
                const id = target.dataset.id;
                deleteCategory(id);
            }
        }
    }

    async function deleteCategory(id) {
        try {
            const response = await fetch(`https://localhost:7007/api/Categories/${id}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (response.ok) { await fetchCategories(); } 
            else { alert('Löschen fehlgeschlagen.'); }
        } catch (error) { console.error('Fehler beim Löschen:', error); }
    }

    document.getElementById('categoryModal').addEventListener('show.bs.modal', (event) => {
        const button = event.relatedTarget; 
        const id = button.dataset.id; 

        if (id) { 
            categoryModalLabel.textContent = 'Kategorie bearbeiten';
            categoryIdInput.value = id;
            nameInput.value = button.dataset.name;
            descriptionInput.value = button.dataset.description;
        } else { 
            categoryModalLabel.textContent = 'Neue Kategorie anlegen';
            categoryForm.reset();
            categoryIdInput.value = '';
        }
    });
    
    categoryForm.addEventListener('submit', handleFormSubmit);
    categoryTableBody.addEventListener('click', handleTableClick);
    fetchCategories();
});