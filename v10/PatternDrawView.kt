package com.smartscreen.control

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.view.MotionEvent
import android.view.View
import kotlin.math.hypot
import kotlin.math.min

/** Native 3x3 drawing pattern. Values are a sequence of distinct 1..9 nodes. */
class PatternDrawView(context: Context) : View(context) {
    var onPatternFinished: ((String) -> Unit)? = null
    private val path = ArrayList<Int>()
    private val circles = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = 0xFF159B83.toInt()
        style = Paint.Style.STROKE
        strokeWidth = 4f * resources.displayMetrics.density
    }
    private val trail = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = 0xFF157D81.toInt()
        strokeWidth = 5f * resources.displayMetrics.density
        strokeCap = Paint.Cap.ROUND
        style = Paint.Style.STROKE
    }
    private var fingerX = -1f
    private var fingerY = -1f
    private fun center(node: Int): Pair<Float, Float> {
        val side = min(width, height) * 0.78f
        val cell = side / 2f
        val left = (width - side) / 2f
        val top = (height - side) / 2f
        return (left + ((node - 1) % 3) * cell) to
            (top + ((node - 1) / 3) * cell)
    }
    override fun onMeasure(widthMeasureSpec: Int, heightMeasureSpec: Int) {
        val desired = (220 * resources.displayMetrics.density).toInt()
        val w = resolveSize(desired, widthMeasureSpec)
        setMeasuredDimension(w, w)
    }
    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        for (i in 1..9) {
            val (x,y) = center(i)
            canvas.drawCircle(x,y, 12f * resources.displayMetrics.density,circles)
        }
        if (path.isNotEmpty()) {
            for (i in 0 until path.size - 1) {
                val (x0,y0)=center(path[i])
                val (x1,y1)=center(path[i+1])
                canvas.drawLine(x0,y0,x1,y1,trail)
            }
            if (fingerX >= 0) {
                val (x,y)=center(path.last())
                canvas.drawLine(x,y,fingerX,fingerY,trail)
            }
        }
    }
    private fun tryAdd(x: Float,y:Float) {
        val radius = min(width,height)*0.16f
        for (i in 1..9) {
            val (cx,cy)=center(i)
            if (hypot(x-cx,y-cy)<=radius && i !in path) {
                path.add(i)
                invalidate()
                break
            }
        }
    }
    override fun onTouchEvent(event: MotionEvent): Boolean {
        when (event.actionMasked) {
            MotionEvent.ACTION_DOWN -> {
                path.clear()
                fingerX=event.x;fingerY=event.y
                tryAdd(event.x,event.y)
                invalidate()
                return true
            }
            MotionEvent.ACTION_MOVE -> {
                fingerX=event.x;fingerY=event.y
                tryAdd(event.x,event.y)
                invalidate()
                return true
            }
            MotionEvent.ACTION_UP -> {
                tryAdd(event.x,event.y)
                fingerX=-1f;fingerY=-1f
                val result=path.joinToString("")
                path.clear()
                invalidate()
                performClick()
                if (result.isNotEmpty()) onPatternFinished?.invoke(result)
                return true
            }
            MotionEvent.ACTION_CANCEL -> {
                path.clear();fingerX=-1f;fingerY=-1f;invalidate();return true
            }
        }
        return true
    }
    override fun performClick(): Boolean {
        super.performClick()
        return true
    }
}
