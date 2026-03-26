window.checkIsAdmin = (token) => {
    if (!token) return false;
    
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
        return role === 'Admin';
    } catch (e) {
        console.error('Error checking admin status:', e);
        return false;
    }
};

window.erpToggleSidebar = (open) => {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebar-overlay');
    if (open) {
        sidebar.classList.add('open');
        overlay.classList.add('open');
    } else {
        sidebar.classList.remove('open');
        overlay.classList.remove('open');
    }
};
