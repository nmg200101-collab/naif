package com.smartscreen.control

import android.app.Activity
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.view.Gravity
import android.view.View
import android.widget.GridLayout
import android.widget.LinearLayout
import android.widget.TextView

/**
 * First screen: nine clear entry points, not nine always-visible tool panels.
 * No inactive feature claims: planned sections are prominently labeled.
 */
class SmartHomeHub(
    private val activity: Activity,
    private val root: LinearLayout,
    private val openSection: (Int) -> Unit
) {
    data class Entry(
        val title: String, val description: String, val symbol: String,
        val color: Int, val ready: Boolean
    )
    private val entries = listOf(
        Entry("الشاشة", "الوقت والإضاءة", "☀", 0xFFF5A623.toInt(), true),
        Entry("الإنترنت", "الحظر والاستهلاك", "◉", 0xFF228BDC.toInt(), true),
        Entry("نقطة البث", "مشاركة واتصال", "⌁", 0xFF16B683.toInt(), true),
        Entry("قفل التطبيقات", "الرموز والحماية", "♢", 0xFF42A34A.toInt(), false),
        Entry("إخفاء التطبيقات", "الخزنة والخصوصية", "◌", 0xFF8A63DD.toInt(), false),
        Entry("الرجوع والتنقل", "الأزرار والإيماءات", "➤", 0xFF8554D8.toInt(), false),
        Entry("الصوت", "المستويات والأوضاع", "♫", 0xFF208ACD.toInt(), true),
        Entry("قفل الهاتف", "قفل فوري وآمن", "▣", 0xFF7546C2.toInt(), true),
        Entry("الإعدادات", "الأذونات والمظهر", "⚙", 0xFF607D8B.toInt(), true),
    )
    private fun dp(v: Int) = (v * activity.resources.displayMetrics.density + 0.5f).toInt()
    private fun background(color: Int, stroke: Int = 0xFFE0EDEA.toInt(), radius: Int = 19) =
        GradientDrawable().apply {
            setColor(color)
            cornerRadius = dp(radius).toFloat()
            setStroke(dp(1), stroke)
        }
    private fun txt(s: String, size: Float, color: Int, bold: Boolean = false) =
        TextView(activity).apply {
            text = s
            textSize = size
            setTextColor(color)
            gravity = Gravity.CENTER
            if (bold) typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        }

    init { build() }

    private fun build() {
        root.removeAllViews()
        root.layoutDirection = View.LAYOUT_DIRECTION_RTL
        val hero = LinearLayout(activity).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(17), dp(19), dp(17), dp(19))
            background = GradientDrawable(
                GradientDrawable.Orientation.TR_BL,
                intArrayOf(0xFF126A99.toInt(), 0xFF18B48F.toInt())
            ).apply { cornerRadius = dp(23).toFloat() }
        }
        hero.addView(txt("✦  مركز التحكم الشامل", 21f, Color.WHITE, true))
        val line = txt("أدوات الهاتف والحماية والخصوصية في مكان واحد", 12f, Color.WHITE)
        line.setPadding(0, dp(6), 0, 0)
        hero.addView(line)
        root.addView(hero)
        val greeting = txt("اختر القسم الذي تريد التحكم به", 16f,
            0xFF173C43.toInt(), true).apply {
            gravity = Gravity.RIGHT
            setPadding(dp(5), dp(18), dp(3), dp(12))
        }
        root.addView(greeting)
        val grid = GridLayout(activity).apply {
            columnCount = 3
            rowCount = 3
            alignmentMode = GridLayout.ALIGN_BOUNDS
            useDefaultMargins = false
        }
        val availableWidth = activity.resources.displayMetrics.widthPixels - dp(36)
        val cellWidth = (availableWidth / 3).coerceAtLeast(dp(88))
        entries.forEachIndexed { index, item ->
            val card = LinearLayout(activity).apply {
                orientation = LinearLayout.VERTICAL
                gravity = Gravity.CENTER
                setPadding(dp(4), dp(10), dp(4), dp(10))
                background = background(Color.WHITE)
                elevation = dp(1).toFloat()
                isClickable = true
                isFocusable = true
                contentDescription = item.title + if (item.ready) "" else "، قيد التطوير"
            }
            val symbol = txt(item.symbol, 29f, item.color, true)
            card.addView(symbol, LinearLayout.LayoutParams(-1, dp(39)))
            val title = txt(item.title, 12.5f, 0xFF152E34.toInt(), true)
            title.setPadding(dp(1), dp(3), dp(1), 0)
            card.addView(title, LinearLayout.LayoutParams(-1, dp(36)))
            val hint = txt(item.description, 10f, 0xFF667D83.toInt())
            card.addView(hint, LinearLayout.LayoutParams(-1, dp(25)))
            if (!item.ready) {
                val plan = txt("قيد التطوير", 9.5f, 0xFF8D66AB.toInt())
                card.addView(plan, LinearLayout.LayoutParams(-1, dp(22)))
            }
            val lp = GridLayout.LayoutParams(
                GridLayout.spec(index / 3), GridLayout.spec(index % 3)
            ).apply {
                width = cellWidth - dp(6)
                height = dp(137)
                setMargins(dp(2), dp(3), dp(2), dp(5))
            }
            grid.addView(card, lp)
            card.setOnClickListener { openSection(index) }
        }
        root.addView(grid)
        val foot = txt("الأدوات المتاحة تعمل محليًا. الإضافات المستقبلية مميزة بوضوح.",
            11f, 0xFF617E80.toInt())
        foot.setPadding(dp(8), dp(14), dp(8), dp(10))
        root.addView(foot)
    }
}
