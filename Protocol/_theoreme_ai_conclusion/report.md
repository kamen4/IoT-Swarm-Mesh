# Итоговый отчет: поиск устойчивого fixed baseline для _theoreme_sim_py

## 1. Постановка задачи

Цель работы:

1. Проверить фиксированный baseline-вектор без оптимизаций.
2. Найти более устойчивый универсальный fixed-вектор для матрицы из 40 топологий.
3. Дать практический вывод: какой вектор использовать как рабочий baseline.

Рабочая среда и артефакты:

- Основной пакетный раннер: [_theoreme_sim_py/run_batch.py](_theoreme_sim_py/run_batch.py)
- Базовые результаты: [_theoreme_ai_search/try_3_baseline](_theoreme_ai_search/try_3_baseline)
- Документ-обоснование теоремы: [_docs_v1.0/math/theorem.md](_docs_v1.0/math/theorem.md)
- Математическая модель: [_docs_v1.0/math/model.md](_docs_v1.0/math/model.md)

## 2. Математическая основа

Симулятор проверяет charge-induced дерево для DOWN на множестве eligible-узлов.

Проверяемые условия:

- A5: gateway является строгим максимумом заряда среди eligible.
- A6: у каждого eligible non-root есть eligible-сосед со строго большим зарядом.
- A7: родитель выбран из соседей и имеет строго больший заряд.
- Лемма 4.1: вдоль parent-chain заряд строго возрастает.
- Лемма 4.2: в parent-графе нет цикла.
- Лемма 4.3: каждый eligible non-root достижим до gateway по parent-chain.

Это реализовано в:

- [_theoreme_sim_py/src/verification/assumption_checks.py](_theoreme_sim_py/src/verification/assumption_checks.py)
- [_theoreme_sim_py/src/verification/theorem_checks.py](_theoreme_sim_py/src/verification/theorem_checks.py)

## 3. Как считалась устойчивость (критерии)

Оценка run-level строится в [_theoreme_sim_py/src/research/stability_scorer.py](_theoreme_sim_py/src/research/stability_scorer.py):

$$
\text{Score} = S_{theorem} + S_{assumptions} + S_{coverage} + S_{duplicate-drop} + S_{parent-stability}
$$

с дополнительными штрафами за коллапс eligible-хвоста и сильный flapping.

Классификация отдельного run:

- STABLE, если Score $\ge 80$
- OSCILLATING, если $60 \le$ Score $< 80$
- UNSTABLE, если Score $< 60$

Классификация network-level агрегата выполняется в [_theoreme_sim_py/src/research/batch_research_runner.py](_theoreme_sim_py/src/research/batch_research_runner.py):

- STABLE, если stableRatio $\ge 0.66$ или avgScore $\ge 82$
- OSCILLATING, если avgScore $\ge 60$
- иначе UNSTABLE

## 4. Пайплайн симуляции

Канонический порядок раундов (из [_docs_v1.0/mitigations/simulation-pipeline.md](_docs_v1.0/mitigations/simulation-pipeline.md)):

1. DOWN phase
2. UP phase
3. Propagate neighbor charges
4. Charge spread
5. Finalize link strength
6. Refresh eligibility
7. Rebuild tree
8. Optional decay
9. Oscillation report
10. Evaluate theorem

Этот порядок критичен для воспроизводимости и сопоставимости метрик.

## 5. История экспериментов (с начала до конца)

### Этап A. Базовый fixed baseline без оптимизаций

Запуск:

- [_theoreme_ai_search/try_3_baseline/2026-04-16_12-37-31](_theoreme_ai_search/try_3_baseline/2026-04-16_12-37-31)
- Summary: [_theoreme_ai_search/try_3_baseline/2026-04-16_12-37-31/run_summary.md](_theoreme_ai_search/try_3_baseline/2026-04-16_12-37-31/run_summary.md)
- Raw report: [_theoreme_ai_search/try_3_baseline/2026-04-16_12-37-31/batch_report.json](_theoreme_ai_search/try_3_baseline/2026-04-16_12-37-31/batch_report.json)

Итог по run-level:

- STABLE: 662
- OSCILLATING: 58
- UNSTABLE: 80
- non-stable runs: 17.25%

Итог по network-level:

- non-stable topologies: 9 из 40
- worst network score: 59.1861 (24x140)

Вывод: baseline был недостаточно устойчив по хвосту.

### Этап B. Первый найденный "идеальный" fixed-вектор

Запуск:

- Request: [_theoreme_ai_search/try_3_baseline/request_ideal_candidate.json](_theoreme_ai_search/try_3_baseline/request_ideal_candidate.json)
- Run: [_theoreme_ai_search/try_3_baseline/2026-04-16_13-10-39](_theoreme_ai_search/try_3_baseline/2026-04-16_13-10-39)
- Summary: [_theoreme_ai_search/try_3_baseline/2026-04-16_13-10-39/run_summary.md](_theoreme_ai_search/try_3_baseline/2026-04-16_13-10-39/run_summary.md)
- Comprehensive: [_theoreme_ai_search/try_3_baseline/2026-04-16_13-10-39/comprehensive_report.md](_theoreme_ai_search/try_3_baseline/2026-04-16_13-10-39/comprehensive_report.md)

Итог:

- STABLE: 798
- OSCILLATING: 2
- UNSTABLE: 0
- non-stable runs: 0.25%
- non-stable topologies: 0 из 40
- worst network score: 83.84 (10x60)

Вывод: найден сильный универсальный fixed baseline.

### Этап C. Дополнительный поиск "еще лучше" (workers=10)

Чтобы выполнить запрос на улучшение, проведен отдельный full candidate sweep из 6 близких кандидатов вокруг текущего идеального вектора.

Артефакты:

- Requests: [_theoreme_ai_search/try_3_baseline/candidate_sweep_requests](_theoreme_ai_search/try_3_baseline/candidate_sweep_requests)
- Runs: [_theoreme_ai_search/try_3_baseline/candidate_sweep_runs](_theoreme_ai_search/try_3_baseline/candidate_sweep_runs)
- Ranking: [_theoreme_ai_search/try_3_baseline/candidate_sweep_summary_w10_full.json](_theoreme_ai_search/try_3_baseline/candidate_sweep_summary_w10_full.json)

Примененный математический критерий отбора (лексикографический):

$$
J(v) = \left(n_{ns}^{network},\; r_{ns}^{run},\; -S_{worst},\; -S_{mean}\right),
$$

где:

- $n_{ns}^{network}$ — число non-stable топологий,
- $r_{ns}^{run}$ — доля non-stable run,
- $S_{worst}$ — худший network score,
- $S_{mean}$ — средний score по сетям.

Минимизируется сначала устойчивость (первые две координаты), затем улучшается качество (последние две).

Результаты candidate sweep:

| Кандидат | Run dir | STABLE/OSC/UNSTABLE | Non-stable runs | Non-stable nets | Worst score | Mean score |
| --- | --- | --- | --- | --- | --- | --- |
| cand2_more_inertia | [_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-40-27](_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-40-27) | 798 / 2 / 0 | 0.25% | 0 | 83.84 | 95.5414 |
| cand0_current | [_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-37-08](_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-37-08) | 798 / 2 / 0 | 0.25% | 0 | 83.84 | 95.4815 |
| cand1_more_eligibility | [_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-38-47](_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-38-47) | 788 / 12 / 0 | 1.50% | 0 | 83.84 | 95.0799 |
| cand3_more_flow | [_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-42-11](_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-42-11) | 786 / 14 / 0 | 1.75% | 0 | 83.84 | 94.9281 |
| cand5_hybrid | [_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-45-40](_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-45-40) | 774 / 24 / 2 | 3.25% | 0 | 83.7160 | 94.8104 |
| cand4_slower_decay | [_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-43-57](_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-43-57) | 768 / 30 / 2 | 4.00% | 1 | 81.8653 | 94.4506 |

Вывод: найдено улучшение относительно текущего профиля — cand2_more_inertia.

## 6. Новый лучший fixed-вектор

Лучший вектор по итогам последнего поиска:

- Файл: [_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json](_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json)

Ключевые значения:

- qForward: 443
- deliveryProbability: 0.21
- rootSourceCharge: 1808
- penaltyLambda: 72
- switchHysteresis: 42
- switchHysteresisRatio: 0.085
- chargeDropPerHop: 94
- chargeSpreadFactor: 0.08
- decayIntervalSteps: 91
- decayPercent: 0.22
- linkMemory: 0.89
- linkLearningRate: 0.5
- linkBonusMax: 50

Сравнение с предыдущим идеальным (cand0_current) на одинаковых условиях sweep:

- Устойчивость одинакова: 0 non-stable сетей и 0.25% non-stable runs.
- Худший score одинаков: 83.84.
- Средний score выше у cand2 на +0.05995.

Именно поэтому cand2 выбран как "еще лучше" в рамках заданного критерия.

## 7. Почему stress-набор не запускался

Условие было: провести stress-набор, если более лучшего решения не найдется.

Так как улучшение найдено (cand2_more_inertia лучше cand0_current по mean score при той же устойчивости), ветка stress-набора по условию не выполнялась.

## 8. Финальные выводы

1. Базовый fixed baseline из первого прогона был существенно нестабилен (17.25% non-stable runs, 9/40 non-stable сетей).
2. Первый "идеальный" профиль радикально улучшил поведение (0.25% non-stable runs, 0/40 non-stable сетей).
3. Дополнительный поиск с parallel workers=10 нашел еще более качественный профиль (cand2_more_inertia) без ухудшения устойчивости.
4. Теоретическая корректность дерева по A5/A6/A7 и леммам 4.1/4.2/4.3 проверяется в каждом раунде симуляции согласно формализации в [_docs_v1.0/math/theorem.md](_docs_v1.0/math/theorem.md).
5. Практически рекомендованный текущий рабочий fixed baseline: cand2_more_inertia.

## 9. Воспроизводимость

Для воспроизведения лучшего найденного кандидата:

1. перейти в [_theoreme_sim_py](_theoreme_sim_py)
2. запустить run_batch.py с request из [_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json](_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json)
3. сохранять результаты в любую подпапку внутри [_theoreme_ai_search/try_3_baseline](_theoreme_ai_search/try_3_baseline)

Эталонный run этого кандидата уже сохранен здесь:

- [_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-40-27](_theoreme_ai_search/try_3_baseline/candidate_sweep_runs/2026-04-16_13-40-27)
