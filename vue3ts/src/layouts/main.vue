<script setup lang="ts">
import { computed } from 'vue'

import { useLayoutStore } from '@/stores/layout'
import Vertical from './vertical.vue'
import Horizontal from './horizontal.vue'
import TwoColumns from './twocolumn.vue'

const layoutType = computed(() => useLayoutStore().layoutType)
</script>

<template>
    <div>
        <Vertical v-if="layoutType === 'vertical' || layoutType === 'semibox'" :layout="layoutType">
            <slot />
        </Vertical>

        <Horizontal v-if="layoutType === 'horizontal'" :layout="layoutType">
            <slot />
        </Horizontal>

        <TwoColumns v-if="layoutType === 'twocolumn'" :layout="layoutType">
            <slot />
        </TwoColumns>
    </div>
</template>
