# بوابة اختبار T1 على الجهاز

الحالة قبل اختبار الجهاز: READY FOR DEVICE TEST

## التحضير
1. افتح فرع rda-t1-foundation من المستودع.
2. افتح مجلد RealDrivingAcademy_Prototype بواسطة Unity 2022.3.62f1.
3. انتظر حتى ينتهي Unity من Import/Compile.
4. تأكد أن Console لا يحتوي على Compile Errors.
5. من القائمة:
   Real Driving Academy > T1 > Build Foundation Scenes
6. بعد اكتمال الإنشاء شغّل:
   Real Driving Academy > T1 > Validate Foundation
7. لا تبدأ الاختبار إلا إذا ظهرت رسالة:
   RDA T1 EDITOR VALIDATION PASSED

## جدول الاختبار الوظيفي
| رقم | الاختبار | النتيجة المطلوبة |
|---|---|---|
| 1 | تشغيل مشهد Welcome | يفتح بدون Error |
| 2 | Continue as Guest | ينتقل إلى MainMenu |
| 3 | Training | يفتح TrainingMenu |
| 4 | Back to Main Menu | يعود للقائمة الرئيسية |
| 5 | Driving Test | يفتح TestMenu |
| 6 | Garage | يفتح Garage |
| 7 | Progress | يفتح Progress |
| 8 | Settings | يفتح Settings |
| 9 | تغيير Master Volume | تتغير القيمة بدون Error |
| 10 | تغيير Steering Sensitivity | تتغير القيمة بدون Error |
| 11 | تغيير Camera Sensitivity | تتغير القيمة بدون Error |
| 12 | تغيير Haptics | تتغير القيمة بدون Error |
| 13 | العودة للقائمة ثم فتح Settings | القيم تبقى كما تم ضبطها |
| 14 | إيقاف Play ثم تشغيله من جديد | يتم تحميل القيم المحفوظة |
| 15 | Training > Start Training Drive | يفتح Stage2_TrainingGround |
| 16 | الضغط على Esc/Back داخل التدريب | يعود إلى MainMenu |
| 17 | End Session | يعود إلى Welcome |
| 18 | مراجعة Console | لا توجد أخطاء حمراء مرتبطة بـRDA |

## معيار النجاح
T1 = PASSED فقط إذا:
- كل الاختبارات 1-18 ناجحة.
- لا توجد Compile Errors.
- لا توجد Missing References في المشاهد المولدة.
- الحفظ يعمل بعد إعادة التشغيل.
- الانتقال بين المشاهد يعمل باستقرار.

بعد نجاح البوابة:
- يتم تثبيت نقطة رجوع باسم RDA-T1-PASSED.
- يبدأ تنفيذ T2 مباشرة.
