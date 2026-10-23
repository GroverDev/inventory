import * as XLSX from 'xlsx';

export const exportToExcel = <T extends object>(data: T[], fileName = 'export.xlsx'): void => {
  const worksheet = XLSX.utils.json_to_sheet(data);
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Sheet1');
  XLSX.writeFile(workbook, fileName);
};

export const exportTemplateToExcel = (headers: string[], fileName = 'template.xlsx'): void => {
  const worksheet = XLSX.utils.aoa_to_sheet([headers]);
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Sheet1');
  XLSX.writeFile(workbook, fileName);
};

export const readExcel = <T>(file: File): Promise<T[]> =>
  new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = e => {
      const workbook = XLSX.read(e.target?.result, { type: 'binary' });
      const worksheet = workbook.Sheets[workbook.SheetNames[0]];
      resolve(XLSX.utils.sheet_to_json(worksheet) as T[]);
    };
    reader.onerror = error => reject(error);
    reader.readAsBinaryString(file);
  });
