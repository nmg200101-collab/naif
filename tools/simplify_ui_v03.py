#!/usr/bin/env python3
"""V0.3: put two primary controls first; preserve everything under More."""
from pathlib import Path
import xml.etree.ElementTree as ET
P=Path.cwd()
A='{http://schemas.android.com/apk/res/android}'
ET.register_namespace('android','http://schemas.android.com/apk/res/android')
layout=P/'app/src/main/res/layout/activity_main.xml'
tree=ET.parse(layout); main=tree.getroot()[0]; old=list(main)
def has(g,name):
    return any(e.get(A+'id','').endswith('/'+name) for e in g.iter())
def group(name): return next(e for e in old if has(e,name))
def idof(g,name): return next(e for e in g.iter() if e.get(A+'id','').endswith('/'+name))
def tagged(g,tag): return next(e for e in g if e.get('style')=='@style/'+tag)
header=old[0]
permission=group('btnPermission'); duration=group('btn5')
timeout=group('spinnerTimeout'); brightness=group('seekBrightness')
tap=group('btnBubbleStart'); wake=group('btnWakeSettings')
quick=next(e for e in old if e.tag=='LinearLayout' and any('اختصار لوحة' in c.get(A+'text','') for c in e))
restore=idof(main,'btnRestoreAll'); footer=old[-1]
header.set(A+'padding','16dp')
header[1].set(A+'text','التحكم في الشاشة بخيارين فقط')
tagged(duration,'SectionTitle').set(A+'text','①  مدة تشغيل الشاشة')
tagged(duration,'SectionHint').set(A+'text','اختر المدة بلمسة واحدة، وتعمل على الهاتف بالكامل بعد منح الإذن.')
idof(duration,'btnStop').set(A+'text','إيقاف الإضاءة واستعادة السكون')
for ident,label in [('btn5','5 دقائق'),('btn15','15 دقيقة'),('btn30','30 دقيقة'),('btn60','ساعة'),('btn120','ساعتان'),('btnAlways','مستمر')]:
    idof(duration,ident).set(A+'text',label)
tagged(tap,'SectionTitle').set(A+'text','②  النقر المزدوج')
tagged(tap,'SectionHint').set(A+'text','اضغط مرتين على الزر العائم لقفل الشاشة، دون التأثير في بقية التطبيقات.')
idof(tap,'btnBubbleStart').set(A+'text','تفعيل النقر المزدوج للقفل')
idof(tap,'btnBubbleStop').set(A+'text','إيقاف الزر العائم')
wake_hint=tagged(wake,'SectionHint')
wake_hint.set(A+'text','تشغيل الشاشة بنقرتين يتطلب ميزة مدمجة في الهاتف.')
wake_btn=idof(wake,'btnWakeSettings')
wake_btn.set(A+'text','إعدادات النقر المزدوج للتشغيل')
wake.remove(wake_hint);wake.remove(wake_btn)
tap.append(wake_hint);tap.append(wake_btn)
lock=idof(tap,'btnLockNow');tap.remove(lock);quick.append(lock)
tagged(permission,'SectionTitle').set(A+'text','الصلاحيات عند الحاجة')
tagged(permission,'SectionHint').set(A+'text','كل صلاحية مطلوبة فقط عند استخدام وظيفتها.')
for item in old: main.remove(item)
for item in (header,duration,tap): main.append(item)
expand=ET.Element('Button',{A+'id':'@+id/btnAdvanced','style':'@style/SecondaryButton',
    A+'layout_marginTop':'14dp',A+'text':'الخيارات الأخرى  ▼'})
main.append(expand)
advanced=ET.Element('LinearLayout',{A+'id':'@+id/advancedPanel',
    A+'layout_width':'match_parent',A+'layout_height':'wrap_content',
    A+'orientation':'vertical',A+'visibility':'gone'})
for item in (permission,timeout,brightness,quick,restore): advanced.append(item)
main.append(advanced)
footer.set(A+'text','الإصدار 0.3.0 • بدون إنترنت • الإيقاظ حسب دعم الهاتف')
main.append(footer)
ET.indent(tree,space='    ')
tree.write(layout,encoding='utf-8',xml_declaration=True)
kt=P/'app/src/main/java/com/smartscreen/control/MainActivity.kt'
s=kt.read_text()
needle='    private lateinit var timeoutSpinner: Spinner\n'
assert needle in s
s=s.replace(needle,needle+'''    private lateinit var advancedPanel: View
    private lateinit var advancedButton: Button
    private var pendingDuration: Int? = null
    private var pendingContinuous = false
''')
needle='        timeoutSpinner.adapter = ArrayAdapter(this,'
assert needle in s
s=s.replace(needle,'''        advancedPanel = findViewById(R.id.advancedPanel)
        advancedButton = findViewById(R.id.btnAdvanced)
        advancedButton.setOnClickListener {
            val show = advancedPanel.visibility != View.VISIBLE
            advancedPanel.visibility = if (show) View.VISIBLE else View.GONE
            advancedButton.text = if (show) "إخفاء الخيارات الأخرى  ▲" else "الخيارات الأخرى  ▼"
        }
'''+needle)
needle='        control.reconcileSession()\n        handler.removeCallbacks(refreshRunnable)'
assert needle in s
s=s.replace(needle,'''        control.reconcileSession()
        if (control.allowed() && (pendingDuration != null || pendingContinuous)) {
            val duration = if (pendingContinuous) null else pendingDuration
            pendingDuration = null
            pendingContinuous = false
            show(control.startSession(duration))
        }
        handler.removeCallbacks(refreshRunnable)''')
needle='    private fun session(minutes: Int?) { show(control.startSession(minutes)) }'
assert needle in s
s=s.replace(needle,'''    private fun session(minutes: Int?) {
        if (!control.allowed()) {
            pendingDuration = minutes
            pendingContinuous = minutes == null
            toast("اسمح بتعديل إعدادات الشاشة لإتمام اختيارك")
            requestWritePermission()
            return
        }
        show(control.startSession(minutes))
    }''')
kt.write_text(s)
gradle=P/'app/build.gradle.kts'
gradle.write_text(gradle.read_text().replace('versionCode = 2','versionCode = 3').replace('versionName = "0.2.0"','versionName = "0.3.0"'))
print('v0.3 UI: two primary sections; optional advanced settings')
