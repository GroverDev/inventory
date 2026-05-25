import { ref, computed, watch, type Ref } from 'vue';

export function usePagination<T>(source: Ref<T[]>, defaultPageSize = 15) {
  const currentPage = ref(1);
  const pageSize = ref(defaultPageSize);

  // Reset to page 1 whenever the source list changes (e.g. after a search/filter)
  watch(source, () => { currentPage.value = 1; });

  const totalItems = computed(() => source.value.length);
  const totalPages = computed(() => Math.max(1, Math.ceil(totalItems.value / pageSize.value)));

  const paginatedItems = computed<T[]>(() => {
    const start = (currentPage.value - 1) * pageSize.value;
    return source.value.slice(start, start + pageSize.value);
  });

  // Window of at most 5 page numbers centred on the current page
  const pageWindow = computed<number[]>(() => {
    const total = totalPages.value;
    const current = currentPage.value;
    const half = 2;
    let start = Math.max(1, current - half);
    let end = Math.min(total, start + 4);
    start = Math.max(1, end - 4);
    const pages: number[] = [];
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  });

  const goToPage = (page: number) => {
    if (page >= 1 && page <= totalPages.value) currentPage.value = page;
  };

  const onPageSizeChange = () => { currentPage.value = 1; };

  return {
    currentPage,
    pageSize,
    totalItems,
    totalPages,
    paginatedItems,
    pageWindow,
    goToPage,
    onPageSizeChange,
  };
}
