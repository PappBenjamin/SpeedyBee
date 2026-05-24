#ifndef ANFIS_MODEL_H
#define ANFIS_MODEL_H

#include <cmath>
#include <algorithm>

namespace ANFIS {
    const float ERROR_MIN = -0.6526310652925633f;
    const float ERROR_MAX = 0.4263955530756801f;
    const float DT_MIN = 0.0319999999999822f;
    const float DT_MAX = 0.0320000000000106f;
    const float TARGET_MIN = -27.54349065519914f;
    const float TARGET_MAX = 66.26529277510215f;

    const float CENTERS_ERR[3] = { -0.5f, -0.3f, -0.09999999999999998f };
    const float SIGMAS_ERR[3]  = { 0.4f, 0.4f, 0.4f };
    const float CENTERS_DT[3]  = { 0.10000000000000009f, 0.30000000000000004f, 0.5f };
    const float SIGMAS_DT[3]   = { 0.4f, 0.4f, 0.4f };

    const int RULE_MAP[9][2] = { { 0, 0 }, { 0, 1 }, { 0, 2 }, { 1, 0 }, { 1, 1 }, { 1, 2 }, { 2, 0 }, { 2, 1 }, { 2, 2 } };

    const float CONSEQUENTS[9][3] = {
        { 0.0f, 0.0f, 0.0f },
        { 0.0f, 0.0f, 0.0f },
        { 0.0f, 0.0f, 0.0f },
        { 0.0f, 0.0f, 0.0f },
        { 0.0f, 0.0f, 0.0f },
        { 0.0f, 0.0f, 0.0f },
        { 0.0f, 0.0f, 0.0f },
        { 0.0f, 0.0f, 0.0f },
        { 0.0f, 0.0f, 0.0f }
    };

    inline float scale(float val, float min_v, float max_v) {
        if (max_v == min_v) return 0.0f;
        return 2.0f * ((val - min_v) / (max_v - min_v)) - 1.0f;
    }

    inline float unscale(float val, float min_v, float max_v) {
        return min_v + ((val + 1.0f) * 0.5f) * (max_v - min_v);
    }

    inline float gaussian(float val, float center, float sigma) {
        if (sigma < 1e-6f) sigma = 1e-6f;
        return std::exp(-0.5f * std::pow((val - center) / sigma, 2));
    }

    inline float predict(float middle_error, float delta_time) {
        float x0 = scale(middle_error, ERROR_MIN, ERROR_MAX);
        float x1 = scale(delta_time, DT_MIN, DT_MAX);

        float mf_err[3];
        for(int i=0; i<3; ++i) mf_err[i] = gaussian(x0, CENTERS_ERR[i], SIGMAS_ERR[i]);

        float mf_dt[3];
        for(int i=0; i<3; ++i) mf_dt[i] = gaussian(x1, CENTERS_DT[i], SIGMAS_DT[i]);

        float weights[9];
        float weight_total = 0.0f;
        for(int i=0; i<9; ++i) {
            weights[i] = mf_err[RULE_MAP[i][0]] * mf_dt[RULE_MAP[i][1]];
            weight_total += weights[i];
        }

        float output_scaled = 0.0f;
        if (weight_total <= 1e-12f) {
            for(int i=0; i<9; ++i) {
                float lin = CONSEQUENTS[i][0]*x0 + CONSEQUENTS[i][1]*x1 + CONSEQUENTS[i][2];
                output_scaled += (1.0f / 9.0f) * lin;
            }
        } else {
            for(int i=0; i<9; ++i) {
                float norm_w = weights[i] / weight_total;
                float lin = CONSEQUENTS[i][0]*x0 + CONSEQUENTS[i][1]*x1 + CONSEQUENTS[i][2];
                output_scaled += norm_w * lin;
            }
        }
        return unscale(output_scaled, TARGET_MIN, TARGET_MAX);
    }
}
#endif
