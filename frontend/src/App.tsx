import { useEffect, useMemo, useState } from 'react'
import { createKitap, getKitap, startGeneration, type Kitap } from './api'
import { PdfViewer } from './PdfViewer'
import './styles.css'

const REQUIRED = 10

export default function App() {
  const [ad, setAd] = useState('')
  const [files, setFiles] = useState<File[]>([])
  const [kitap, setKitap] = useState<Kitap | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const ready = ad.trim().length > 0 && files.length === REQUIRED && !busy
  const processing = kitap?.durum === 'Isleniyor' || busy

  useEffect(() => {
    if (!kitap || kitap.durum !== 'Isleniyor') return
    const timer = window.setInterval(async () => {
      try {
        const next = await getKitap(kitap.id)
        setKitap(next)
        if (next.durum === 'Failed') setError(next.hataMesaji ?? 'E-kitap oluşturulamadı.')
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Durum okunamadı.')
      }
    }, 1200)
    return () => window.clearInterval(timer)
  }, [kitap])

  const pdfUrl = useMemo(
    () => (kitap?.durum === 'Tamamlandi' ? `/api/kitaplar/${kitap.id}/pdf` : null),
    [kitap],
  )

  function onPick(list: FileList | null) {
    if (!list) return
    const picked = Array.from(list)
    const invalid = picked.find((file) => !file.name.toLowerCase().endsWith('.docx'))
    if (invalid) {
      setError('Yalnızca .docx dosyaları seçilebilir.')
      return
    }
    const next = [...files, ...picked].slice(0, REQUIRED)
    setFiles(next)
    setError(next.length < REQUIRED ? null : null)
    setKitap(null)
  }

  function move(index: number, direction: -1 | 1) {
    const target = index + direction
    if (target < 0 || target >= files.length) return
    const next = [...files]
    const [item] = next.splice(index, 1)
    next.splice(target, 0, item)
    setFiles(next)
  }

  async function generate() {
    setError(null)
    setBusy(true)
    setKitap(null)
    try {
      const created = await createKitap(ad.trim(), files)
      const started = await startGeneration(created.id)
      setKitap(started)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Kitap oluşturulamadı.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page">
      <header className="top">
        <p className="eyebrow">Bildiri derlemesi</p>
        <h1>E-kitap atölyesi</h1>
        <p className="lead">On Word bildirisini sırayla tek bir PDF kitapta birleştirin.</p>
      </header>

      <main className="layout">
        <section className="panel">
          <label className="field">
            <span>Kitap adı</span>
            <input value={ad} onChange={(e) => setAd(e.target.value)} placeholder="Örneğin 2026 Sempozyum Bildirileri" maxLength={200} />
          </label>

          <label className="drop">
            <input type="file" accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document" multiple onChange={(e) => { onPick(e.target.files); e.target.value = '' }} />
            <strong>{files.length}/10 dosya</strong>
            <span>.docx bildirilerini seçin. Sıra, listedeki yukarı-aşağı ile değişir.</span>
          </label>

          <ol className="files">
            {files.map((file, index) => (
              <li key={`${file.name}-${index}`}>
                <span className="order">{index + 1}</span>
                <span className="name">{file.name}</span>
                <span className="moves">
                  <button type="button" onClick={() => move(index, -1)} disabled={index === 0} aria-label="Yukarı taşı">↑</button>
                  <button type="button" onClick={() => move(index, 1)} disabled={index === files.length - 1} aria-label="Aşağı taşı">↓</button>
                </span>
              </li>
            ))}
            {Array.from({ length: REQUIRED - files.length }).map((_, index) => (
              <li key={`empty-${index}`} className="empty">
                <span className="order">{files.length + index + 1}</span>
                <span className="name">Dosya bekleniyor</span>
              </li>
            ))}
          </ol>

          <button className="primary" type="button" disabled={!ready || kitap?.durum === 'Isleniyor'} onClick={generate}>
            Kitabı Oluştur
          </button>
          {error && <p className="error" role="alert">{error}</p>}
        </section>

        <section className="panel viewer">
          {processing && kitap?.durum !== 'Tamamlandi' && (
            <div className="status" role="status">
              <div className="pulse" />
              <p>{kitap?.asama ?? 'Dosyalar yükleniyor'}</p>
              <small>Bildiriler okunuyor, iletişim bilgileri temizleniyor ve sayfalar birleştiriliyor.</small>
            </div>
          )}

          {kitap?.durum === 'Tamamlandi' && pdfUrl && (
            <PdfViewer url={pdfUrl} title={kitap.ad} papers={kitap.bildiriler} />
          )}

          {!processing && kitap?.durum !== 'Tamamlandi' && (
            <div className="placeholder">
              <p>Kitap burada açılacak.</p>
              <small>Adı yazın, 10 bildiriyi sıralayın ve oluşturmayı başlatın.</small>
            </div>
          )}
        </section>
      </main>
    </div>
  )
}
