<script lang="ts">
import { defineComponent } from "vue";
import SideBar from "./components/SideBar.vue";
import RootPage from "./pages/RootPage.vue";

export default defineComponent({
  components: {
    SideBar,
    RootPage,
  },
});
</script>

<template>
  <side-bar></side-bar>
  <root-page></root-page>
</template>

<style scoped></style>
