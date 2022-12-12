<script lang="ts">
import { defineComponent } from "vue";
import type { PropType } from "vue";
import type EventStream from "@/types/EventStream";

export default defineComponent({
  data() {
    return { currentHash: window.location.hash };
  },
  props: {
    eventStreams: {
      type: Array as PropType<EventStream[]>,
      required: true,
    },
  },
  methods: {
    isActive(eventStream: EventStream) {
      return (
        this.currentPath === "/" + eventStream.name ||
        this.currentPath.startsWith("/" + eventStream.name + "/")
      );
    },
  },
  computed: {
    currentPath() {
      return this.currentHash.slice(1) || "/";
    },
  },
  mounted() {
    window.addEventListener("hashchange", () => {
      this.currentHash = window.location.hash;
    });
  },
});
</script>

<template>
  <div class="d-flex flex-column flex-shrink-0 p-3 text-bg-dark sidebar">
    <a
      href="/"
      class="d-flex align-items-center mb-3 mb-md-0 me-md-auto text-white text-decoration-none"
    >
      <img
        src="@/assets/log-otter-grayscale.svg"
        class="me-2"
        width="32"
        alt="LogOtter"
      />
      <span class="fs-5">Event Stream Viewer</span>
    </a>
    <hr />
    <ul class="nav nav-pills flex-column mb-auto">
      <li>
        <a
          href="#"
          class="nav-link text-white"
          :class="{ active: currentPath == '/' }"
        >
          <i class="bi-house-door me-2"></i>
          Home
        </a>
      </li>
      <li v-for="eventStream in eventStreams" :key="eventStream.name">
        <a
          :href="'#/' + eventStream.name"
          class="nav-link text-white"
          :class="{ active: isActive(eventStream) }"
        >
          <i class="bi-journals me-2"></i>
          {{ eventStream.name }}
        </a>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.sidebar {
  width: 280px;
}
</style>
