package com.smartscreen.control

import android.app.Activity
import android.content.Context
import android.content.Intent
import android.media.AudioManager
import android.net.Uri
import android.provider.Settings
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.view.Gravity
import android.view.View
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast

/**
 * Honest feature entry pages. Only actions wired to Android are clickable.
 * App lock/hide/gesture replacements are roadmap items, not simulated functions.
 */
class FeatureGateway(
    private val activity: Activity,
    private val root: LinearLayout,
    private val lockNow: () -> Unit
) {
    private fun dp(v: Int) = (v * activity.resources.displayMetrics.density + .5f).toInt()
    private fun box() = LinearLayout(activity).apply {
        orientation = LinearLayout.VERTICAL
        setPadding(dp(16),dp(18),dp(16),dp(18))
        layoutDirection = View.LAYOUT_DIRECTION_RTL
        background = GradientDrawable().apply {
            setColor(Color.WHITE)
            cornerRadius = dp(20).toFloat()
            setStroke(dp(1),0xFFD7E8E6.toInt())
        }
        layoutParams = LinearLayout.LayoutParams(-1,-2)
    }
    private fun text(value:String, size:Float, bold:Boolean = false) =
        TextView(activity).apply {
            text=value
            textSize=size
            setTextColor(if(bold) 0xFF143A3D.toInt() else 0xFF627D81.toInt())
            setPadding(dp(5),dp(8),dp(5),dp(8))
            gravity=Gravity.RIGHT
            if(bold) typeface=Typeface.create(Typeface.DEFAULT,Typeface.BOLD)
        }
    private fun button(value:String, fn:()->Unit) = Button(activity).apply {
        text=value
        isAllCaps=false
        textSize=14f
        setTextColor(Color.WHITE)
        backgroundTintList=android.content.res.ColorStateList.valueOf(0xFF16A77E.toInt())
        setOnClickListener { fn() }
    }
    private fun launch(action:String) {
        try { activity.startActivity(Intent(action)) }
        catch (_: Exception) {
            try { activity.startActivity(Intent(Settings.ACTION_SETTINGS)) }
            catch (_: Exception) {
                Toast.makeText(activity,"هذه الإعدادات غير متاحة على الهاتف",
                    Toast.LENGTH_LONG).show()
            }
        }
    }
    fun open(feature:Int) {
        root.removeAllViews()
        val card=box()
        when(feature) {
            3 -> {
                card.addView(text("قفل التطبيقات",22f,true))
                card.addView(text("قيد التطوير: رمز موحد أو لكل تطبيق، PIN، كلمة مرور، نقش وبصمة الهاتف. هذه النسخة لا تدّعي قفل تطبيقات أخرى بالفعل.",14f))
                card.addView(button("فتح إعدادات أمان الهاتف") {
                    launch(Settings.ACTION_SECURITY_SETTINGS)
                })
            }
            4 -> {
                card.addView(text("إخفاء التطبيقات",22f,true))
                card.addView(text("قيد التطوير: خزنة محمية واختصار إعدادات سريعة، ثم إخفاء الأيقونات فقط إن كان المشغل أو الملف الخاص يدعمه. لم تُخف أي تطبيقات بعد.",14f))
                card.addView(button("فتح إعدادات المشغل الرئيسي") {
                    launch(Settings.ACTION_HOME_SETTINGS)
                })
            }
            5 -> {
                card.addView(text("الرجوع والتنقل",22f,true))
                card.addView(text("قيد التطوير: رجوع عائم قابل للتحريك وأزرار رجوع/الرئيسية/التطبيقات الأخيرة وإيماءات جانبية. يحتاج تنفيذًا وإذن إمكانية الوصول منفصلًا.",14f))
                card.addView(button("فتح إعدادات إمكانية الوصول") {
                    launch(Settings.ACTION_ACCESSIBILITY_SETTINGS)
                })
            }
            6 -> {
                card.addView(text("التحكم بالصوت",22f,true))
                card.addView(text("التحكم المباشر في مستوى صوت الوسائط. أوضاع الصوت وعدم الإزعاج المتقدمة ستضاف لاحقًا بعد اختبار الصلاحيات.",14f))
                val audio=activity.getSystemService(Context.AUDIO_SERVICE) as AudioManager
                card.addView(button("رفع صوت الوسائط") {
                    try { audio.adjustStreamVolume(AudioManager.STREAM_MUSIC,
                        AudioManager.ADJUST_RAISE, AudioManager.FLAG_SHOW_UI) }
                    catch (_: Exception) {
                        Toast.makeText(activity,"يتعذر تعديل الصوت الآن",Toast.LENGTH_SHORT).show()
                    }
                })
                card.addView(button("خفض صوت الوسائط") {
                    try { audio.adjustStreamVolume(AudioManager.STREAM_MUSIC,
                        AudioManager.ADJUST_LOWER, AudioManager.FLAG_SHOW_UI) }
                    catch (_: Exception) {
                        Toast.makeText(activity,"يتعذر تعديل الصوت الآن",Toast.LENGTH_SHORT).show()
                    }
                })
                card.addView(button("إعدادات الصوت والنغمات") {
                    launch(Settings.ACTION_SOUND_SETTINGS)
                })
            }
            7 -> {
                card.addView(text("قفل الهاتف",22f,true))
                card.addView(text("قفل فوري عبر مسؤول الجهاز بعد منح الصلاحية. رمز الهاتف وبصمته محميان بإعدادات أندرويد الأصلية.",14f))
                card.addView(button("قفل الشاشة الآن") { lockNow() })
                card.addView(button("إعدادات قفل أندرويد") {
                    launch(Settings.ACTION_SECURITY_SETTINGS)
                })
            }
            8 -> {
                card.addView(text("الإعدادات والصلاحيات",22f,true))
                card.addView(text("تحكم في صلاحيات التطبيق من نظام أندرويد، وإعدادات الخصوصية والحماية. إعدادات داخلية إضافية قيد التطوير.",14f))
                card.addView(button("صلاحيات التطبيق") {
                    try {
                        activity.startActivity(Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS,
                            Uri.parse("package:" + activity.packageName)))
                    } catch (_: Exception) { launch(Settings.ACTION_SETTINGS) }
                })
            }
        }
        root.addView(card)
    }
}
