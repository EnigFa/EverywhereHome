import { useEffect, useRef, useState } from "react";
import { CATEGORIES } from "../api/client";
import { useLanguage } from "../i18n";

type Props = { values: number[]; onChange: (values: number[]) => void; label?: string };
const labels = {
  uk: ["Гарні краєвиди", "Невеликі квартири", "Великі квартири", "Хостели", "Luxe", "У центрі міста", "Сільська місцевість", "Від дизайнера"],
  en: ["Great views", "Small apartments", "Large apartments", "Hostels", "Luxe", "City center", "Countryside", "Designer"]
};
export function CategoryPicker({ values, onChange, label }: Props) {
  const { language, t } = useLanguage(); const [open, setOpen] = useState(false); const rootRef = useRef<HTMLDivElement>(null);
  useEffect(() => { const onClick=(event:MouseEvent)=>{if(rootRef.current&&!rootRef.current.contains(event.target as Node))setOpen(false)}; document.addEventListener("mousedown",onClick); return()=>document.removeEventListener("mousedown",onClick); },[]);
  const toggle=(value:number)=>onChange(values.includes(value)?values.filter(item=>item!==value):[...values,value]);
  const title=label ?? t("categories"); const buttonLabel=values.length===0?title:`${title} (${values.length})`;
  return <div className="dropdown" ref={rootRef}><span className="dropdown-label">{title}</span><button type="button" className="dropdown-toggle" onClick={()=>setOpen(v=>!v)}>{buttonLabel}</button>{open&&<div className="dropdown-panel">{CATEGORIES.map((item,index)=><label key={item.value} className="flag"><input type="checkbox" checked={values.includes(item.value)} onChange={()=>toggle(item.value)}/>{labels[language][index]}</label>)}</div>}</div>;
}
