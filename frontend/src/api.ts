export type Bildiri = {
  id: string
  sira: number
  dosyaAdi: string
  baslik: string | null
  sayfaBaslangic: number | null
}

export type Kitap = {
  id: string
  ad: string
  durum: 'Taslak' | 'Isleniyor' | 'Tamamlandi' | 'Failed'
  hataMesaji: string | null
  asama: string | null
  olusturmaTarihi: string
  bildiriler: Bildiri[]
}

async function readError(response: Response) {
  try {
    const body = await response.json()
    return body.message ?? 'İşlem tamamlanamadı.'
  } catch {
    return 'İşlem tamamlanamadı.'
  }
}

export async function createKitap(ad: string, files: File[]) {
  const form = new FormData()
  form.append('ad', ad)
  files.forEach((file) => form.append('files', file))
  const response = await fetch('/api/kitaplar', { method: 'POST', body: form })
  if (!response.ok) throw new Error(await readError(response))
  return (await response.json()) as Kitap
}

export async function startGeneration(id: string) {
  const response = await fetch(`/api/kitaplar/${id}/olustur`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return (await response.json()) as Kitap
}

export async function getKitap(id: string) {
  const response = await fetch(`/api/kitaplar/${id}`)
  if (!response.ok) throw new Error(await readError(response))
  return (await response.json()) as Kitap
}
