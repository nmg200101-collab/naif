package com.smartscreen.control

import android.app.Activity
import android.app.AlertDialog
import android.content.Intent
import android.content.pm.ApplicationInfo
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.os.Build
import android.provider.Settings
import android.text.InputType
import android.view.Gravity
import android.view.View
import android.widget.Button
import android.widget.CheckBox
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import android.widget.Toast

/**
 * App-lock management. Master management needs verification on every new visit.
 * No hidden activity access, no irreversible app disablement or device-policy takeover.
 */
class AppLockPanel(
    private val activity: Activity,
    private val root: LinearLayout
) {
    private val vault = AppLockStore(activity)
    private val selected = mutableSetOf<String>()
    private var showAll = false
    private var adminUntil = 0L
    private val ink = 0xFF193D43.toInt()
    private val quiet = 0xFF607D80.toInt()
    private val green = 0xFF159D79.toInt()
    private val pale = 0xFFF0FAF5.toInt()
    private val apps by lazy {
        @Suppress("DEPRECATION")
        activity.packageManager.getInstalledApplications(0)
            .filter {
                it.packageName != activity.packageName &&
                    (it.flags and ApplicationInfo.FLAG_SYSTEM) == 0 &&
                    activity.packageManager.getLaunchIntentForPackage(it.packageName) != null
            }
            .sortedBy { it.loadLabel(activity.packageManager).toString().lowercase() }
            .take(240)
    }
    private fun dp(value: Int) =
        (value * activity.resources.displayMetrics.density + .5f).toInt()
    private fun card() = LinearLayout(activity).apply {
        orientation = LinearLayout.VERTICAL
        layoutDirection = View.LAYOUT_DIRECTION_RTL
        setPadding(dp(14), dp(14), dp(14), dp(14))
        background = GradientDrawable().apply {
            setColor(Color.WHITE)
            cornerRadius = dp(19).toFloat()
            setStroke(dp(1), 0xFFDCEAE6.toInt())
        }
        layoutParams = LinearLayout.LayoutParams(-1, -2).apply {
            topMargin = dp(12)
        }
    }
    private fun text(value: String, size: Float = 13f, color: Int = quiet,
                     bold: Boolean = false) = TextView(activity).apply {
        text = value
        textSize = size
        setTextColor(color)
        gravity = Gravity.RIGHT or Gravity.CENTER_VERTICAL
        setPadding(dp(4), dp(6), dp(4), dp(6))
        if (bold) typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
    }
    private fun button(title: String, highlighted: Boolean = false, click: () -> Unit) =
        Button(activity).apply {
            text = title
            isAllCaps = false
            textSize = 13f
            setTextColor(if (highlighted) Color.WHITE else ink)
            backgroundTintList = android.content.res.ColorStateList.valueOf(
                if (highlighted) green else 0xFFE4F3ED.toInt())
            setOnClickListener { click() }
        }
    private fun row() = LinearLayout(activity).apply {
        orientation = LinearLayout.HORIZONTAL
        layoutDirection = View.LAYOUT_DIRECTION_RTL
    }
    private fun warning(message: String) {
        Toast.makeText(activity, message, Toast.LENGTH_LONG).show()
    }
    private fun adminReady() = vault.hasMaster() &&
        System.currentTimeMillis() < adminUntil

    fun refresh() {
        if (root.visibility == View.VISIBLE) render()
    }

    fun render() {
        root.removeAllViews()
        val intro = card()
        intro.addView(text("قفل التطبيقات", 22f, ink, true))
        intro.addView(text("رمز موحد أو خاص لكل تطبيق، مع الأرقام أو كلمة مرور أو نقش مرسوم. "
            + "خدمة إمكانية الوصول اختيارية، ولا تحل محل حماية أندرويد الأصلية.",
            12f))
        val monitoring = AppLockAccessibilityService.connected
        intro.addView(text(if (monitoring)
            "● خدمة قفل التطبيقات مفعّلة"
            else "○ خدمة قفل التطبيقات غير مفعّلة",
            13f, if (monitoring) green else 0xFFC26133.toInt(), true))
        intro.addView(button("فتح صلاحية إمكانية الوصول", true) {
            try { activity.startActivity(Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS)) }
            catch (_: Exception) { warning("تعذر فتح إعدادات إمكانية الوصول") }
        })
        intro.addView(text("لا تلتقط الخدمة نصوص التطبيقات أو كلمات مرورها؛ "
            + "تراقب اسم التطبيق النشط فقط. قد توجد حالات تأخير أو تجاوز "
            + "في تعدد النوافذ أو بحسب نظام الهاتف.", 11f))
        root.addView(intro)

        if (!vault.hasMaster()) {
            val setup = card()
            setup.addView(text("الخطوة الأولى: تعيين رمز حماية موحد", 17f, ink, true))
            setup.addView(text("أنشئ رمزًا موحدًا قبل اختيار التطبيقات التي تريد حمايتها. "
                + "احتفظ بالرمز في مكان آمن.", 12f))
            setup.addView(button("إنشاء الرمز الموحد", true) {
                chooseMethod(null)
            })
            root.addView(setup)
            return
        }
        if (!adminReady()) {
            val gate = card()
            gate.addView(text("إدارة القفل محمية", 18f, ink, true))
            gate.addView(text("تحقق من الرمز الموحد لعرض الإعدادات وتغيير "
                + "الحماية أو رموز التطبيقات. تنتهي جلسة الإدارة بعد خمس دقائق.", 12f))
            gate.addView(button("فتح إدارة قفل التطبيقات", true) {
                promptAdmin()
            })
            root.addView(gate)
            return
        }

        val management = card()
        management.addView(text("إدارة الحماية", 18f, ink, true))
        management.addView(text("التطبيقات المحمية: " + vault.protectedPackages().size, 13f))
        management.addView(button("تغيير الرمز الموحد") { chooseMethod(null) })
        if (Build.VERSION.SDK_INT >= 28) {
            val biometrics = CheckBox(activity).apply {
                text = "السماح بالمصادقة الحيوية للهاتف (عند الدعم)"
                textSize = 13f
                isChecked = vault.biometricAllowed()
                setOnCheckedChangeListener { _, checked ->
                    vault.setBiometricAllowed(checked)
                }
            }
            management.addView(biometrics)
            management.addView(text("تُستخدم واجهة بصمة أندرويد الرسمية عند دعم الهاتف. "
                + "إذا رفض النظام تشغيل نافذة البصمة، استخدم الرمز.", 11f))
        }
        root.addView(management)

        val appsCard = card()
        appsCard.addView(text("اختيار التطبيقات", 18f, ink, true))
        appsCard.addView(text("حدد التطبيقات ثم اختر «قفل المحدد» أو «إلغاء قفل المحدد». "
            + "يمكن تعيين رمز خاص لكل تطبيق من صفّه.", 12f))
        val controls = row()
        controls.addView(button("تحديد الكل") {
            selected.clear()
            selected.addAll(apps.map { it.packageName })
            render()
        }, LinearLayout.LayoutParams(0, dp(51), 1f))
        controls.addView(button("إلغاء التحديد") {
            selected.clear()
            render()
        }, LinearLayout.LayoutParams(0, dp(51), 1f))
        appsCard.addView(controls)
        val bulk = row()
        bulk.addView(button("قفل المحدد", true) {
            if (selected.isEmpty()) { warning("حدد تطبيقًا أولًا"); return@button }
            selected.toList().forEach { vault.protect(it) }
            selected.clear()
            render()
        }, LinearLayout.LayoutParams(0, dp(51), 1f))
        bulk.addView(button("إلغاء قفل المحدد") {
            if (selected.isEmpty()) { warning("حدد تطبيقًا أولًا"); return@button }
            selected.toList().forEach { vault.unprotect(it) }
            selected.clear()
            render()
        }, LinearLayout.LayoutParams(0, dp(51), 1f))
        appsCard.addView(bulk)
        appsCard.addView(text("المحدد: " + selected.size + " من " + apps.size,
            12f, quiet))

        val shown = if (showAll) apps else apps.take(8)
        for (app in shown) {
            val pkg = app.packageName
            val name = app.loadLabel(activity.packageManager).toString()
            val item = LinearLayout(activity).apply {
                orientation = LinearLayout.VERTICAL
                layoutDirection = View.LAYOUT_DIRECTION_RTL
                setPadding(dp(8), dp(9), dp(8), dp(9))
                background = GradientDrawable().apply {
                    setColor(pale)
                    cornerRadius = dp(11).toFloat()
                }
                layoutParams = LinearLayout.LayoutParams(-1, -2).apply {
                    topMargin = dp(6)
                }
            }
            val top = row()
            val checkbox = CheckBox(activity).apply {
                text = name
                textSize = 13f
                isChecked = pkg in selected
                setOnCheckedChangeListener { _, checked ->
                    if (checked) selected.add(pkg) else selected.remove(pkg)
                }
            }
            top.addView(checkbox, LinearLayout.LayoutParams(-1, dp(49)))
            item.addView(top)
            val state = when {
                !vault.isProtected(pkg) -> "غير محمي"
                vault.hasSeparate(pkg) -> "مقفل برمز مستقل"
                else -> "مقفل بالرمز الموحد"
            }
            item.addView(text(state, 12f, quiet))
            val actions = row()
            actions.addView(button(
                if (vault.isProtected(pkg)) "إلغاء قفل التطبيق" else "حماية التطبيق"
            ) {
                if (vault.isProtected(pkg)) vault.unprotect(pkg) else vault.protect(pkg)
                render()
            }, LinearLayout.LayoutParams(0, dp(46), 1f))
            if (vault.isProtected(pkg)) {
                actions.addView(button(
                    if (vault.hasSeparate(pkg)) "العودة للموحد" else "رمز خاص"
                ) {
                    if (vault.hasSeparate(pkg)) {
                        vault.useMaster(pkg)
                        render()
                    } else chooseMethod(pkg)
                }, LinearLayout.LayoutParams(0, dp(46), 1f))
            }
            item.addView(actions)
            appsCard.addView(item)
        }
        if (apps.size > 8) {
            appsCard.addView(button(if (showAll) "عرض أقل" else "عرض جميع التطبيقات") {
                showAll = !showAll
                render()
            })
        }
        root.addView(appsCard)
        val note = card()
        note.addView(text("معلومات مهمة", 16f, ink, true))
        note.addView(text("قفل التطبيقات حماية إضافية وليست ضمانًا لعزل التطبيق. "
            + "يمكن إيقاف خدمة إمكانية الوصول من إعدادات الهاتف، "
            + "وقد تسمح بعض الأجهزة برؤية التطبيق لحظيًا قبل ظهور القفل. "
            + "لا نقفل تطبيق إعدادات النظام أو تطبيقنا نفسه.", 12f))
        root.addView(note)
    }

    private fun promptAdmin() {
        val kind = vault.masterKind() ?: return
        val wrapper = LinearLayout(activity).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(16), dp(8), dp(16), dp(8))
        }
        if (kind == LockCrypto.PATTERN) {
            val hint = text("ارسم النقش الموحد للدخول إلى إدارة القفل", 13f)
            wrapper.addView(hint)
            val pattern = PatternDrawView(activity)
            wrapper.addView(pattern, LinearLayout.LayoutParams(-1, dp(200)))
            val dialog = AlertDialog.Builder(activity)
                .setTitle("التحقق من الهوية")
                .setView(wrapper)
                .setNegativeButton("إلغاء", null)
                .create()
            pattern.onPatternFinished = { code ->
                if (verifyAdmin(code)) {
                    dialog.dismiss()
                    render()
                } else hint.text = "النقش غير صحيح أو المحاولات معلقة"
            }
            dialog.show()
            return
        }
        val input = secretEdit(kind, "الرمز الموحد")
        wrapper.addView(input)
        val dialog = AlertDialog.Builder(activity)
            .setTitle("فتح إدارة القفل")
            .setView(wrapper)
            .setPositiveButton("فتح", null)
            .setNegativeButton("إلغاء", null)
            .create()
        dialog.setOnShowListener {
            dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                val entered = input.text.toString()
                input.text.clear()
                if (verifyAdmin(entered)) {
                    dialog.dismiss()
                    render()
                } else warning("رمز غير صحيح أو عدد المحاولات تجاوز الحد")
            }
        }
        dialog.show()
    }

    private fun verifyAdmin(code: String): Boolean {
        val tag = "__admin_lock_settings"
        if (vault.lockoutRemainingMs(tag) > 0) return false
        val correct = vault.verifyMaster(code)
        vault.recordAttempt(tag, correct)
        if (correct) adminUntil = System.currentTimeMillis() + 5 * 60_000L
        return correct
    }

    private fun chooseMethod(pkg: String?) {
        val options = arrayOf("رمز أرقام PIN", "كلمة مرور", "نقش 3×3")
        val values = arrayOf(LockCrypto.PIN, LockCrypto.PASSWORD, LockCrypto.PATTERN)
        AlertDialog.Builder(activity)
            .setTitle(if (pkg == null) "طريقة الرمز الموحد" else "طريقة رمز التطبيق")
            .setItems(options) { _, index ->
                createCredential(values[index], pkg)
            }
            .setNegativeButton("إلغاء", null)
            .show()
    }

    private fun secretEdit(kind: String, caption: String): EditText =
        EditText(activity).apply {
            hint = caption
            textSize = 16f
            setSingleLine(true)
            inputType = if (kind == LockCrypto.PIN)
                InputType.TYPE_CLASS_NUMBER or InputType.TYPE_NUMBER_VARIATION_PASSWORD
            else InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_PASSWORD
            importantForAutofill = View.IMPORTANT_FOR_AUTOFILL_NO
        }

    private fun createCredential(kind: String, pkg: String?) {
        var firstPattern = ""
        var secondPattern = ""
        val parent = LinearLayout(activity).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(15), dp(4), dp(15), dp(4))
        }
        var firstInput: EditText? = null
        var secondInput: EditText? = null
        val hint = text(if (kind == LockCrypto.PATTERN)
            "ارسم نقشًا من 4 إلى 9 نقاط مختلفة مرتين."
            else if (kind == LockCrypto.PIN)
            "أدخل رمزًا من 4 إلى 12 رقمًا مرتين."
            else "أدخل كلمة مرور من 6 إلى 64 حرفًا مرتين.", 12f)
        parent.addView(hint)
        if (kind == LockCrypto.PATTERN) {
            val a = PatternDrawView(activity)
            val b = PatternDrawView(activity)
            a.onPatternFinished = { firstPattern = it; hint.text = "أعد رسم النقش للتأكيد" }
            b.onPatternFinished = { secondPattern = it; hint.text = "اضغط حفظ لتأكيد النقش" }
            parent.addView(a, LinearLayout.LayoutParams(-1, dp(145)))
            parent.addView(b, LinearLayout.LayoutParams(-1, dp(145)))
        } else {
            firstInput = secretEdit(kind, "الرمز الجديد")
            secondInput = secretEdit(kind, "تأكيد الرمز")
            parent.addView(firstInput)
            parent.addView(secondInput)
        }
        val scroll = ScrollView(activity)
        scroll.addView(parent)
        val dialog = AlertDialog.Builder(activity)
            .setTitle(if (pkg == null) "الرمز الموحد" else "رمز خاص للتطبيق")
            .setView(scroll)
            .setPositiveButton("حفظ", null)
            .setNegativeButton("إلغاء", null)
            .create()
        dialog.setOnShowListener {
            dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                val first = firstInput?.text?.toString() ?: firstPattern
                val second = secondInput?.text?.toString() ?: secondPattern
                if (!LockCrypto.isValid(kind, first) || first != second) {
                    warning("الرمز غير صالح أو التأكيد غير مطابق")
                    return@setOnClickListener
                }
                try {
                    if (pkg == null) {
                        vault.setMaster(kind, first)
                        adminUntil = System.currentTimeMillis() + 5 * 60_000L
                    } else vault.setIndividual(pkg, kind, first)
                    firstInput?.text?.clear()
                    secondInput?.text?.clear()
                    dialog.dismiss()
                    render()
                } catch (_: Exception) {
                    warning("تعذر حفظ الرمز، أعد المحاولة")
                }
            }
        }
        dialog.show()
    }
}
