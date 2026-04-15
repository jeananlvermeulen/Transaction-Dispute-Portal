import { createContext, useContext, useState, ReactNode } from 'react'
import { getTranslations, Translations } from '../utils/translations'

interface LanguageContextType {
  lang: string
  setLang: (lang: string) => void
  t: Translations
}

const LanguageContext = createContext<LanguageContextType>({
  lang: 'en',
  setLang: () => {},
  t: getTranslations('en'),
})

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<string>(
    () => localStorage.getItem('customer_lang') ?? 'en'
  )

  const setLang = (code: string) => {
    setLangState(code)
    localStorage.setItem('customer_lang', code)
  }

  return (
    <LanguageContext.Provider value={{ lang, setLang, t: getTranslations(lang) }}>
      {children}
    </LanguageContext.Provider>
  )
}

export function useLanguage() {
  return useContext(LanguageContext)
}
