import { createApp } from "vue";
import AppContainer from "./AppContainer.vue";
import "./styles/main.scss";
import "bootstrap-icons/font/bootstrap-icons.css";
import * as dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";

dayjs.extend(relativeTime);

createApp(AppContainer).mount("#app");
