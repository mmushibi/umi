class DataTable {
    constructor(containerId, options = {}) {
        this.container = document.getElementById(containerId);
        this.options = {
            pageSize: 10,
            searchable: true,
            sortable: true,
            filterable: true,
            ...options
        };
        this.data = [];
        this.filteredData = [];
        this.currentPage = 1;
        this.sortColumn = null;
        this.sortDirection = 'asc';
        this.filters = {};
        
        this.init();
    }

    init() {
        this.createTableStructure();
        this.bindEvents();
    }

    createTableStructure() {
        this.container.innerHTML = `
            <div class="data-table-wrapper">
                ${this.options.searchable ? this.createSearchBar() : ''}
                ${this.options.filterable ? this.createFilterBar() : ''}
                <div class="table-responsive">
                    <table class="data-table">
                        <thead class="table-header"></thead>
                        <tbody class="table-body"></tbody>
                    </table>
                </div>
                <div class="table-footer">
                    <div class="pagination-info"></div>
                    <div class="pagination-controls"></div>
                </div>
            </div>
        `;
    }

    createSearchBar() {
        return `
            <div class="search-bar">
                <input type="text" class="search-input" placeholder="Search..." />
                <button class="search-btn">🔍</button>
            </div>
        `;
    }

    createFilterBar() {
        return `
            <div class="filter-bar">
                <div class="filter-controls"></div>
                <button class="clear-filters-btn">Clear Filters</button>
            </div>
        `;
    }

    bindEvents() {
        if (this.options.searchable) {
            const searchInput = this.container.querySelector('.search-input');
            const searchBtn = this.container.querySelector('.search-btn');
            
            searchInput?.addEventListener('input', (e) => this.handleSearch(e.target.value));
            searchBtn?.addEventListener('click', () => this.handleSearch(searchInput.value));
        }

        if (this.options.filterable) {
            const clearBtn = this.container.querySelector('.clear-filters-btn');
            clearBtn?.addEventListener('click', () => this.clearFilters());
        }
    }

    setData(data) {
        this.data = data;
        this.filteredData = [...data];
        this.currentPage = 1;
        this.render();
    }

    handleSearch(searchTerm) {
        if (!searchTerm.trim()) {
            this.filteredData = [...this.data];
        } else {
            this.filteredData = this.data.filter(row => 
                Object.values(row).some(value => 
                    value?.toString().toLowerCase().includes(searchTerm.toLowerCase())
                )
            );
        }
        this.currentPage = 1;
        this.render();
    }

    handleSort(column) {
        if (this.sortColumn === column) {
            this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            this.sortColumn = column;
            this.sortDirection = 'asc';
        }

        this.filteredData.sort((a, b) => {
            const aVal = a[column] || '';
            const bVal = b[column] || '';
            
            if (typeof aVal === 'number' && typeof bVal === 'number') {
                return this.sortDirection === 'asc' ? aVal - bVal : bVal - aVal;
            }
            
            const comparison = aVal.toString().localeCompare(bVal.toString());
            return this.sortDirection === 'asc' ? comparison : -comparison;
        });

        this.render();
    }

    handleFilter(column, value) {
        if (value) {
            this.filters[column] = value;
        } else {
            delete this.filters[column];
        }

        this.applyFilters();
    }

    applyFilters() {
        this.filteredData = this.data.filter(row => {
            return Object.entries(this.filters).every(([column, filterValue]) => {
                const rowValue = row[column]?.toString().toLowerCase();
                return rowValue?.includes(filterValue.toString().toLowerCase());
            });
        });
        this.currentPage = 1;
        this.render();
    }

    clearFilters() {
        this.filters = {};
        this.filteredData = [...this.data];
        this.currentPage = 1;
        this.render();
    }

    render() {
        this.renderHeader();
        this.renderBody();
        this.renderPagination();
    }

    renderHeader() {
        if (!this.options.columns) return;

        const header = this.container.querySelector('.table-header');
        const headerRow = document.createElement('tr');

        this.options.columns.forEach(column => {
            const th = document.createElement('th');
            th.className = 'table-header-cell';
            
            if (this.options.sortable && column.sortable !== false) {
                th.classList.add('sortable');
                th.innerHTML = `
                    ${column.title}
                    <span class="sort-indicator ${this.sortColumn === column.key ? this.sortDirection : ''}">
                        ${this.sortColumn === column.key ? (this.sortDirection === 'asc' ? '↑' : '↓') : '↕'}
                    </span>
                `;
                th.addEventListener('click', () => this.handleSort(column.key));
            } else {
                th.textContent = column.title;
            }

            headerRow.appendChild(th);
        });

        header.innerHTML = '';
        header.appendChild(headerRow);
    }

    renderBody() {
        const tbody = this.container.querySelector('.table-body');
        tbody.innerHTML = '';

        const startIndex = (this.currentPage - 1) * this.options.pageSize;
        const endIndex = startIndex + this.options.pageSize;
        const pageData = this.filteredData.slice(startIndex, endIndex);

        if (pageData.length === 0) {
            const emptyRow = document.createElement('tr');
            emptyRow.innerHTML = `<td colspan="${this.options.columns?.length || 1}" class="no-data">No data available</td>`;
            tbody.appendChild(emptyRow);
            return;
        }

        pageData.forEach(row => {
            const tr = document.createElement('tr');
            tr.className = 'table-row';

            this.options.columns?.forEach(column => {
                const td = document.createElement('td');
                td.className = 'table-cell';
                
                if (column.render) {
                    td.innerHTML = column.render(row[column.key], row);
                } else {
                    td.textContent = row[column.key] || '';
                }

                tr.appendChild(td);
            });

            tbody.appendChild(tr);
        });
    }

    renderPagination() {
        const totalPages = Math.ceil(this.filteredData.length / this.options.pageSize);
        const paginationInfo = this.container.querySelector('.pagination-info');
        const paginationControls = this.container.querySelector('.pagination-controls');

        // Update info
        const startIndex = (this.currentPage - 1) * this.options.pageSize + 1;
        const endIndex = Math.min(this.currentPage * this.options.pageSize, this.filteredData.length);
        paginationInfo.textContent = `Showing ${startIndex}-${endIndex} of ${this.filteredData.length} items`;

        // Update controls
        paginationControls.innerHTML = '';

        // Previous button
        const prevBtn = document.createElement('button');
        prevBtn.className = 'pagination-btn';
        prevBtn.textContent = '←';
        prevBtn.disabled = this.currentPage === 1;
        prevBtn.addEventListener('click', () => this.goToPage(this.currentPage - 1));
        paginationControls.appendChild(prevBtn);

        // Page numbers
        const startPage = Math.max(1, this.currentPage - 2);
        const endPage = Math.min(totalPages, this.currentPage + 2);

        if (startPage > 1) {
            this.addPageButton(1, paginationControls);
            if (startPage > 2) {
                const ellipsis = document.createElement('span');
                ellipsis.textContent = '...';
                ellipsis.className = 'pagination-ellipsis';
                paginationControls.appendChild(ellipsis);
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            this.addPageButton(i, paginationControls);
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                const ellipsis = document.createElement('span');
                ellipsis.textContent = '...';
                ellipsis.className = 'pagination-ellipsis';
                paginationControls.appendChild(ellipsis);
            }
            this.addPageButton(totalPages, paginationControls);
        }

        // Next button
        const nextBtn = document.createElement('button');
        nextBtn.className = 'pagination-btn';
        nextBtn.textContent = '→';
        nextBtn.disabled = this.currentPage === totalPages;
        nextBtn.addEventListener('click', () => this.goToPage(this.currentPage + 1));
        paginationControls.appendChild(nextBtn);
    }

    addPageButton(pageNum, container) {
        const btn = document.createElement('button');
        btn.className = `pagination-btn ${pageNum === this.currentPage ? 'active' : ''}`;
        btn.textContent = pageNum;
        btn.addEventListener('click', () => this.goToPage(pageNum));
        container.appendChild(btn);
    }

    goToPage(page) {
        const totalPages = Math.ceil(this.filteredData.length / this.options.pageSize);
        if (page >= 1 && page <= totalPages) {
            this.currentPage = page;
            this.render();
        }
    }

    refresh() {
        this.render();
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DataTable;
}
