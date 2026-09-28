import { useEffect, useState } from 'react'
import type { Bildiri } from './api'

type Props = {
  url: string
  title: string
  papers: Bildiri[]
}

export function PdfViewer({ url, title, papers }: Props) {
  const [src, setSrc] = useState<string | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    let objectUrl: string | null = null
    setSrc(null)
    setLoadError(null)

    fetch(url)
      .then(async (response) => {
        if (!response.ok) throw new Error('PDF alınamadı.')
        const blob = await response.blob()
        return new Blob([blob], { type: 'application/pdf' })
      })
      .then((blob) => {
        objectUrl = URL.createObjectURL(blob)
        if (active) setSrc(objectUrl)
      })
      .catch((err: unknown) => {
        if (active) setLoadError(err instanceof Error ? err.message : 'PDF açılamadı.')
      })

    return () => {
      active = false
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [url])

  return (
    <div className="book">
      <div className="book-bar">
        <div>
          <strong>{title}</strong>
          <small>E-kitap hazır</small>
        </div>
        <div className="book-actions">
          <a href={url} download={`${title}.pdf`}>İndir</a>
        </div>
      </div>

      <ul className="toc">
        {papers.map((paper) => (
          <li key={paper.id}>
            <span>{paper.baslik ?? paper.dosyaAdi}</span>
            <em>{paper.sayfaBaslangic ?? '—'}</em>
          </li>
        ))}
      </ul>

      {loadError && <p className="error">{loadError}</p>}
      {src && <iframe className="book-frame" title={title} src={src} />}
    </div>
  )
}
