import { Component, useEffect, useMemo, useState, type ReactNode } from 'react'
import { createKitap, getKitap, startGeneration, type Kitap } from './api'
import { PdfViewer } from './PdfViewer'
import './styles.css'

const REQUIRED = 10

class ViewBoundary extends Component<{ children: ReactNode }, { message: string | null }> {
  state = { message: null as string | null }

  static getDerivedStateFromError(error: Error) {
    return { message: error.message || 'Kitap görüntülenemedi.' }
  }

  render() {
    if (this.state.message) return <p className="error">{this.state.message}</p>
    return this.props.children
  }
}

async function isDocx(file: File) {
  if (!file.name.toLowerCase().endsWith('.docx')) return false
  const header = new Uint8Array(await file.slice(0, 4).arrayBuffer())
  return header.length === 4 && header[0] === 0x50 && header[1] === 0x4b && header[2] === 0x03 && header[3] === 0x04
}

export default function App() {
  const [ad, setAd] = useState('')
  const [files, setFiles] = useState<File[]>([])
  const [kitap, setKitap] = useState<Kitap | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const [dragOver, setDragOver] = useState(false)
  const named = ad.trim().length > 0
  const ready = named && files.length === REQUIRED && !busy
  const processing = kitap?.durum === 'Isleniyor' || busy
  const finished = kitap?.durum === 'Tamamlandi'

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

  async function onPick(list: FileList | null) {
    if (!list || list.length === 0) return
    const picked = Array.from(list)
    const checks = await Promise.all(picked.map(async (file) => ({ file, ok: await isDocx(file) })))
    const valid = checks.filter((item) => item.ok).map((item) => item.file)
    if (valid.length === 0) {
      setError('Seçilen dosyalar geçerli Word (.docx) belgesi değil.')
      return
    }
    setFiles((current) => {
      const base = valid.length >= REQUIRED ? [] : current
      return [...base, ...valid].slice(0, REQUIRED)
    })
    setError(valid.length < picked.length ? 'Geçersiz dosyalar listeye eklenmedi. Yalnızca gerçek .docx belgeleri kullanılır.' : null)
    setKitap(null)
  }

  function removeFile(index: number) {
    setFiles((current) => current.filter((_, item) => item !== index))
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
    <div className={`page${processing ? ' is-working' : ''}${ready ? ' is-ready' : ''}${finished ? ' is-done' : ''}`}>
      <header className="topbar">
        <div className="brand">
          <span className="mark" aria-hidden="true">Ek</span>
          <div>
            <p className="eyebrow">Bildiri derlemesi</p>
            <h1>E-kitap</h1>
          </div>
        </div>
        <ol className="steps">
          <li className={named ? 'done' : 'current'}>Kitap adı</li>
          <li className={files.length === REQUIRED ? 'done' : named ? 'current' : ''}>10 bildiri</li>
          <li className={finished ? 'done' : ready ? 'current' : ''}>PDF</li>
        </ol>
      </header>

      <main className="layout">
        <section className="panel composer">
          <label className="field">
            <span>Kitap adı</span>
            <input value={ad} onChange={(e) => setAd(e.target.value)} placeholder="Örneğin 2026 Sempozyum Bildirileri" maxLength={200} />
          </label>

          <label
            className={dragOver ? 'drop over' : 'drop'}
            onDragOver={(event) => { event.preventDefault(); setDragOver(true) }}
            onDragLeave={() => setDragOver(false)}
            onDrop={(event) => { event.preventDefault(); setDragOver(false); onPick(event.dataTransfer.files) }}
          >
            <input type="file" accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document" multiple onChange={(e) => { onPick(e.target.files); e.target.value = '' }} />
            <strong>Bildirileri bırakın veya seçin</strong>
            <span>Yalnızca .docx. Sırayı oklarla değiştirin.</span>
          </label>

          <div className="meter" aria-label={`${files.length} / ${REQUIRED} dosya`}>
            <span style={{ width: `${files.length * 10}%` }} />
          </div>
          <p className="count">{files.length} / {REQUIRED} dosya</p>

          {files.length === 0 ? (
            <p className="empty-note">Henüz bildiri yok.</p>
          ) : (
            <ol className="files">
              {files.map((file, index) => (
                <li key={`${file.name}-${file.size}-${index}`}>
                  <span className="order">{index + 1}</span>
                  <span className="name">
                    <b>{file.name}</b>
                    <small>{formatSize(file.size)}</small>
                  </span>
                  <span className="moves">
                    <button type="button" onClick={() => move(index, -1)} disabled={index === 0} aria-label="Yukarı taşı">↑</button>
                    <button type="button" onClick={() => move(index, 1)} disabled={index === files.length - 1} aria-label="Aşağı taşı">↓</button>
                    <button type="button" onClick={() => removeFile(index)} aria-label="Kaldır">×</button>
                  </span>
                </li>
              ))}
            </ol>
          )}

          <div className="action">
            <button className="primary" type="button" disabled={!ready || kitap?.durum === 'Isleniyor'} onClick={generate}>
              {processing ? 'Oluşturuluyor…' : 'Kitabı Oluştur'}
            </button>
            {!ready && !processing && (
              <p className="hint">
                {!named && 'Kitap adını yazın. '}
                {files.length < REQUIRED && `${REQUIRED - files.length} dosya daha seçin.`}
              </p>
            )}
            {error && <p className="error" role="alert">{error}</p>}
          </div>
        </section>

        <section className="panel viewer">
          {processing && !finished && (
            <div className="status" role="status">
              <div className="pulse" />
              <p>{kitap?.asama ?? 'Dosyalar yükleniyor'}</p>
              <small>İletişim bilgileri temizleniyor, sayfalar birleştiriliyor.</small>
            </div>
          )}

          {finished && pdfUrl && (
            <ViewBoundary>
              <PdfViewer url={pdfUrl} title={kitap.ad} papers={kitap.bildiriler} />
            </ViewBoundary>
          )}

          {!processing && !finished && (
            <div className="placeholder">
              <p className="stage-count" aria-hidden="true">{files.length}<small>/10</small></p>
              <p>Kitap burada açılacak.</p>
              <ol>
                <li className={named ? 'done' : ''}>Adı yazın</li>
                <li className={files.length === REQUIRED ? 'done' : ''}>On bildiriyi sıralayın</li>
                <li>Oluşturun, görüntüleyin, indirin</li>
              </ol>
            </div>
          )}
        </section>
      </main>
    </div>
  )
}

function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
