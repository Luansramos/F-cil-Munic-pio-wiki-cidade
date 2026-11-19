const PADRAO_URL_HTTPS = /^https:\/\/([^\s/@?#]+)(?:[/?#]\S*)?$/i;

const PADRAO_DATA = /^(\d{4})-(\d{2})-(\d{2})$/;

export function ehUrlSegura(url: string | undefined): url is string {
  return url !== undefined && PADRAO_URL_HTTPS.test(url);
}

export function enderecoExibido(url: string): string | undefined {
  const encontrado = PADRAO_URL_HTTPS.exec(url);
  return encontrado ? encontrado[1] : undefined;
}

export function formatarDataConsulta(data: string | undefined): string | undefined {
  if (data === undefined) {
    return undefined;
  }
  const encontrada = PADRAO_DATA.exec(data);
  if (!encontrada) {
    return undefined;
  }
  const mes = Number(encontrada[2]);
  const dia = Number(encontrada[3]);
  if (mes < 1 || mes > 12 || dia < 1 || dia > 31) {
    return undefined;
  }
  return `${encontrada[3]}/${encontrada[2]}/${encontrada[1]}`;
}
